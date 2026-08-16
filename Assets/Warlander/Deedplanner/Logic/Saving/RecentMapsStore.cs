using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class RecentMapsStore
    {
        private const int MaxEntries = 30;

        private const string StoreFileName = "recent-maps.json";
        private const string ThumbnailFolderName = "Thumbnails";
        private const string PlayerPrefsKey = "RecentMaps";

        private readonly List<RecentMapEntry> _entries = new List<RecentMapEntry>();

        public IReadOnlyList<RecentMapEntry> Entries => _entries;

        public event Action Changed = delegate { };

        public RecentMapsStore()
        {
            Load();
        }

        public void Record(MapLocation location, byte[] thumbnailJpeg)
        {
            _entries.RemoveAll(e => SameLocation(e.Location, location));
            _entries.Insert(0, new RecentMapEntry(location, DateTime.UtcNow, thumbnailJpeg != null && thumbnailJpeg.Length > 0));

            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
            }

            if (thumbnailJpeg != null && thumbnailJpeg.Length > 0)
            {
                WriteThumbnailCache(location, thumbnailJpeg);
            }

            Save();
            Changed();
        }

        public void Remove(MapLocation location)
        {
            _entries.RemoveAll(e => SameLocation(e.Location, location));
            Save();
            Changed();
        }

        public byte[] LoadThumbnail(MapLocation location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return null;
#else
            string path = ThumbnailPathFor(location);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
#endif
        }

        private static bool SameLocation(MapLocation a, MapLocation b)
        {
            return a.BackendId == b.BackendId && a.Locator == b.Locator;
        }

        private static string ThumbnailPathFor(MapLocation location)
        {
            using (SHA1 sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(location.BackendId + "|" + location.Locator));
                StringBuilder hex = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                {
                    hex.Append(hash[i].ToString("x2"));
                }

                return Path.Combine(ThumbnailFolder, hex + ".jpg");
            }
        }

        private static string ThumbnailFolder
        {
            get
            {
                string folder = Path.Combine(Application.persistentDataPath, ThumbnailFolderName);
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private void WriteThumbnailCache(MapLocation location, byte[] jpeg)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            File.WriteAllBytes(ThumbnailPathFor(location), jpeg);
#endif
        }

        private void Save()
        {
            var dto = new StoreDto();
            foreach (RecentMapEntry entry in _entries)
            {
                dto.entries.Add(new EntryDto
                {
                    backendId = entry.Location.BackendId,
                    locator = entry.Location.Locator,
                    displayName = entry.Location.DisplayName,
                    lastOpenedUtc = entry.LastOpenedUtc.ToString("o"),
                    hasThumbnail = entry.HasThumbnail
                });
            }

            string json = JsonUtility.ToJson(dto);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
#else
            File.WriteAllText(Path.Combine(Application.persistentDataPath, StoreFileName), json);
#endif
        }

        private void Load()
        {
            string json = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                json = PlayerPrefs.GetString(PlayerPrefsKey);
            }
#else
            string path = Path.Combine(Application.persistentDataPath, StoreFileName);
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
            }
#endif
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            StoreDto dto = JsonUtility.FromJson<StoreDto>(json);
            if (dto?.entries == null)
            {
                return;
            }

            foreach (EntryDto entry in dto.entries)
            {
                if (!DateTime.TryParse(entry.lastOpenedUtc, out DateTime lastOpened))
                {
                    lastOpened = DateTime.MinValue;
                }

                var location = new MapLocation(entry.backendId, entry.locator, entry.displayName);
                _entries.Add(new RecentMapEntry(location, lastOpened, entry.hasThumbnail));
            }
        }

        [Serializable]
        private class StoreDto
        {
            public List<EntryDto> entries = new List<EntryDto>();
        }

        [Serializable]
        private class EntryDto
        {
            public string backendId;
            public string locator;
            public string displayName;
            public string lastOpenedUtc;
            public bool hasThumbnail;
        }
    }
}
