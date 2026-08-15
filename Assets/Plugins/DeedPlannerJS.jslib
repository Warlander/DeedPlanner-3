mergeInto(LibraryManager.library, {

    LoadResourceNative: function(location) {
        var locationString = UTF8ToString(location);
        var request = new XMLHttpRequest();
        request.open('GET', locationString, false);
        request.overrideMimeType('text\/plain; charset=x-user-defined');
        request.send(null);
        
        var response = request.responseText;
        
        var pointer = _malloc(response.length);
        var dataHeap = new Uint8Array(HEAPU8.buffer, pointer, response.length);
        for (var i=0; i < response.length; i++) {
            dataHeap[i] = response.charCodeAt(i) & 0xff;
        }
        
        window.lastLoadedResourceLength = response.length;
        return pointer;
    },
    
    GetLastLoadedResourceLengthNative : function() {
        return window.lastLoadedResourceLength;
    },
    
    GetMapLocationString : function() {
        var urlString = window.location.href;
        var url = new URL(urlString);
        var mapLocation = url.searchParams.get("map");
        if (mapLocation == null) {
            mapLocation = "";
        }

        var bufferSize = lengthBytesUTF8(mapLocation) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(mapLocation, buffer, bufferSize);
        return buffer;
    },

    DownloadNative : function(name, content) {
        var jsName = UTF8ToString(name);
        var jsContent = UTF8ToString(content);

        var element = document.createElement('a');
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(jsContent));
        element.setAttribute('download', jsName);

        element.style.display = 'none';
        document.body.appendChild(element);

        element.click();

        document.body.removeChild(element);
    },

    DownloadBinaryNative : function(name, data, length) {
        var jsName = UTF8ToString(name);
        var bytes = HEAPU8.slice(data, data + length);

        var blob = new Blob([bytes], { type: 'application/octet-stream' });
        var url = URL.createObjectURL(blob);

        var element = document.createElement('a');
        element.setAttribute('href', url);
        element.setAttribute('download', jsName);

        element.style.display = 'none';
        document.body.appendChild(element);

        element.click();

        document.body.removeChild(element);
        setTimeout(function() { URL.revokeObjectURL(url); }, 0);
    },
    
    UploadNative : function(objectCallbackName, methodCallbackName) {
        var jsObjectCallbackName = UTF8ToString(objectCallbackName);
        var jsMethodCallbackName = UTF8ToString(methodCallbackName);
        
        var element = document.createElement('input');
        element.setAttribute('type', 'file');

        element.style.display = 'none';
        document.body.appendChild(element);

        element.click();
        
        document.body.removeChild(element);

        element.addEventListener('input', function(evt) {
            var file = element.files[0];
            if (file) {
                var reader = new FileReader();

                reader.onload = function(evt) {
                    var result = reader.result;
                    SendMessage(jsObjectCallbackName, jsMethodCallbackName, result);
                };

                reader.onerror = function(evt) {
                    SendMessage(jsObjectCallbackName, jsMethodCallbackName, '');
                };

                reader.readAsText(file, "UTF-8");
            }
        });
    },
    
    PromptNative : function(message, defaultInput) {
        var jsMessage = UTF8ToString(message);
        var jsDefaultInput = UTF8ToString(defaultInput);
        var jsContent = prompt(jsMessage, jsDefaultInput);

        if (jsContent == null) {
            jsContent = "";
        }

        var bufferSize = lengthBytesUTF8(jsContent) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(jsContent, buffer, bufferSize);
        return buffer;
    },

    LocalStorageSetItemNative : function(key, value) {
        try {
            localStorage.setItem(UTF8ToString(key), UTF8ToString(value));
            return 1;
        } catch (e) {
            return 0;
        }
    },

    LocalStorageGetItemNative : function(key) {
        var value = localStorage.getItem(UTF8ToString(key));
        if (value === null) {
            value = "";
        }

        var bufferSize = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(value, buffer, bufferSize);
        return buffer;
    },

    LocalStorageHasItemNative : function(key) {
        return localStorage.getItem(UTF8ToString(key)) !== null ? 1 : 0;
    },

    LocalStorageRemoveItemNative : function(key) {
        localStorage.removeItem(UTF8ToString(key));
    },

    LocalStorageTotalSizeNative : function() {
        var total = 0;
        for (var i = 0; i < localStorage.length; i++) {
            var key = localStorage.key(i);
            var value = localStorage.getItem(key);
            total += key.length + (value ? value.length : 0);
        }
        return total;
    }

});