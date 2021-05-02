using System;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class ArchedBridgeType : IBridgeType
    {
        private static readonly int[] Height0 = {};
        private static readonly int[] Height1 = {20};
        private static readonly int[] Height2 = {15, 30};
        private static readonly int[] Height3 = {10, 30, 40};
        private static readonly int[] Height4 = {8, 28, 48, 57};
        private static readonly int[] Height5 = {6, 22, 42, 59, 65};
        private static readonly int[] Height6 = {5, 20, 40, 60, 75, 80};
        private static readonly int[] Height7 = {4, 17, 35, 55, 73, 85, 90};
        private static readonly int[] Height8 = {4, 15, 32, 52, 72, 89, 101, 105};
        private static readonly int[] Height9 = {3, 13, 29, 48, 68, 86, 102, 112, 115};
        private static readonly int[] Height10 = {3, 12, 27, 45, 65, 85, 103, 117, 126, 129};
        private static readonly int[] Height11 = {3, 11, 24, 41, 60, 80, 99, 116, 129, 138, 141};
        private static readonly int[] Height12 = {3, 10, 23, 39, 57, 77, 97, 116, 132, 144, 152, 155};
        private static readonly int[] Height13 = {2, 10, 21, 36, 54, 73, 93, 112, 130, 145, 156, 164, 166};
        private static readonly int[] Height14 = {2, 9, 20, 34, 51, 70, 90, 110, 129, 146, 160, 171, 178, 180};
        private static readonly int[] Height15 = {2, 8, 18, 32, 48, 66, 86, 106, 125, 144, 160, 173, 183, 189, 191};
        private static readonly int[] Height16 = {2, 8, 17, 30, 46, 63, 83, 103, 123, 142, 159, 175, 188, 197, 203, 205};
        private static readonly int[] Height17 = {2, 7, 16, 28, 43, 60, 79, 98, 118, 138, 157, 174, 188, 201, 209, 215, 217};
        private static readonly int[] Height18 = {2, 7, 15, 27, 41, 58, 76, 95, 115, 135, 155, 173, 189, 203, 215, 223, 229, 230};
        private static readonly int[] Height19 = {2, 7, 15, 26, 39, 55, 72, 91, 111, 131, 151, 170, 187, 203, 217, 228, 236, 241, 242};
        private static readonly int[] Height20 = {2, 6, 14, 24, 37, 53, 70, 88, 108, 128, 148, 167, 186, 203, 218, 231, 242, 249, 254, 256};
        private static readonly int[] Height21 = {1, 6, 13, 23, 36, 50, 67, 85, 104, 124, 144, 164, 183, 201, 217, 232, 244, 254, 262, 266, 268};
        private static readonly int[] Height22 = {1, 6, 13, 22, 34, 49, 65, 82, 101, 121, 141, 161, 180, 199, 217, 233, 247, 259, 268, 275, 280, 281};
        private static readonly int[] Height23 = {1, 5, 12, 21, 33, 47, 62, 79, 97, 117, 137, 157, 176, 196, 214, 231, 247, 260, 272, 281, 288, 292, 293};
        private static readonly int[] Height24 = {1, 5, 12, 21, 32, 45, 60, 77, 95, 114, 133, 153, 173, 193, 212, 230, 247, 262, 275, 286, 295, 301, 305, 306};
        private static readonly int[] Height25 = {1, 5, 11, 20, 30, 43, 58, 74, 91, 110, 129, 149, 169, 189, 208, 227, 245, 261, 275, 288, 299, 307, 314, 317, 319};
        private static readonly int[] Height26 = {1, 5, 11, 19, 29, 42, 56, 72, 89, 107, 126, 146, 166, 186, 206, 225, 243, 260, 276, 290, 302, 313, 321, 327, 331, 332};
        private static readonly int[] Height27 = {1, 5, 10, 18, 28, 40, 54, 69, 86, 104, 123, 142, 162, 182, 202, 221, 240, 258, 275, 290, 304, 316, 326, 334, 339, 343, 344};
        private static readonly int[] Height28 = {1, 4, 10, 18, 27, 39, 52, 67, 84, 101, 120, 139, 159, 179, 199, 218, 238, 256, 274, 290, 305, 318, 330, 340, 347, 353, 356, 357};
        private static readonly int[] Height29 = {1, 4, 10, 17, 26, 38, 51, 65, 81, 98, 116, 135, 155, 175, 195, 215, 234, 253, 271, 288, 304, 319, 332, 343, 352, 360, 365, 368, 369};
        private static readonly int[] Height30 = {1, 4, 9, 17, 26, 37, 49, 63, 79, 96, 114, 132, 152, 171, 191, 211, 231, 250, 269, 287, 304, 319, 334, 346, 357, 366, 373, 378, 382, 383};
        private static readonly int[] Height31 = {1, 4, 9, 16, 25, 35, 48, 61, 77, 93, 110, 129, 148, 168, 187, 207, 227, 247, 266, 284, 302, 318, 333, 347, 359, 370, 379, 386, 391, 394, 395};
        private static readonly int[] Height32 = {1, 4, 9, 16, 24, 34, 46, 60, 75, 91, 108, 126, 145, 164, 184, 204, 224, 244, 263, 282, 300, 317, 333, 348, 362, 374, 384, 393, 399, 404, 407, 408};
        private static readonly int[] Height33 = {1, 4, 9, 15, 23, 33, 45, 58, 73, 88, 105, 123, 141, 161, 180, 200, 220, 240, 260, 279, 297, 315, 332, 348, 362, 375, 387, 397, 405, 412, 417, 419, 420};
        private static readonly int[] Height34 = {1, 4, 8, 15, 23, 32, 44, 57, 71, 86, 103, 120, 138, 157, 177, 197, 217, 237, 257, 276, 295, 313, 331, 347, 363, 377, 390, 401, 411, 419, 425, 430, 433, 434};
        private static readonly int[] Height35 = {1, 4, 8, 14, 22, 32, 43, 55, 69, 84, 100, 117, 135, 154, 173, 193, 213, 233, 253, 272, 292, 310, 329, 346, 362, 377, 391, 403, 414, 424, 432, 438, 442, 445, 446};
        private static readonly int[] Height36 = {1, 3, 8, 14, 21, 31, 41, 54, 67, 82, 98, 115, 132, 151, 170, 190, 209, 229, 249, 269, 289, 308, 326, 344, 361, 377, 392, 405, 417, 428, 437, 445, 451, 455, 458, 459};
        private static readonly int[] Height37 = {1, 3, 8, 13, 21, 30, 40, 52, 66, 80, 96, 112, 130, 148, 167, 186, 206, 226, 246, 266, 285, 305, 323, 342, 359, 376, 391, 406, 419, 431, 441, 450, 458, 464, 468, 470, 471};
        private static readonly int[] Height38 = {1, 3, 7, 13, 20, 29, 39, 51, 64, 78, 93, 110, 127, 145, 164, 183, 202, 222, 242, 262, 282, 302, 321, 339, 357, 375, 391, 406, 420, 433, 445, 455, 464, 471, 477, 481, 484, 484};
        private static readonly int[] Height39 = {1, 3, 7, 13, 20, 28, 38, 50, 62, 76, 91, 107, 124, 142, 160, 179, 199, 218, 238, 258, 278, 298, 317, 336, 355, 373, 389, 405, 420, 434, 447, 458, 468, 477, 484, 489, 493, 496, 497};
        private static readonly int[] Height40 = {1, 3, 7, 12, 19, 28, 38, 49, 61, 75, 89, 105, 122, 139, 157, 176, 195, 215, 235, 255, 275, 295, 314, 334, 352, 371, 388, 405, 420, 435, 449, 461, 472, 482, 490, 497, 503, 507, 509, 510};
        private static readonly int[] Height41 = {1, 3, 7, 12, 19, 27, 37, 48, 60, 73, 87, 103, 119, 136, 154, 173, 192, 211, 231, 251, 271, 291, 311, 330, 349, 368, 386, 403, 419, 435, 449, 462, 475, 485, 495, 503, 510, 515, 519, 521, 522};
        private static readonly int[] Height42 = {1, 3, 7, 12, 19, 27, 36, 47, 58, 71, 86, 101, 117, 134, 152, 170, 189, 208, 228, 248, 268, 288, 308, 327, 347, 365, 384, 401, 418, 434, 450, 464, 477, 489, 499, 509, 517, 523, 529, 532, 535, 535};
        private static readonly int[] Height43 = {1, 3, 7, 12, 18, 26, 35, 45, 57, 70, 84, 99, 114, 131, 149, 167, 185, 205, 224, 244, 264, 284, 304, 324, 343, 362, 381, 399, 416, 433, 449, 464, 478, 491, 502, 513, 522, 530, 536, 541, 545, 547, 548};
        private static readonly int[] Height44 = {1, 3, 6, 11, 18, 25, 34, 45, 56, 68, 82, 97, 112, 129, 146, 164, 182, 201, 221, 240, 260, 280, 300, 320, 340, 359, 378, 397, 415, 432, 448, 464, 479, 492, 505, 516, 526, 535, 543, 549, 554, 558, 560, 561};
        private static readonly int[] Height45 = {1, 3, 6, 11, 17, 25, 34, 44, 55, 67, 80, 95, 110, 126, 143, 161, 179, 198, 217, 237, 257, 277, 297, 316, 336, 356, 375, 394, 412, 430, 447, 463, 478, 493, 506, 518, 530, 540, 548, 556, 562, 567, 570, 572, 573};
        private static readonly int[] Height46 = {1, 3, 6, 11, 17, 24, 33, 43, 54, 66, 79, 93, 108, 124, 141, 158, 176, 195, 214, 233, 253, 273, 293, 313, 333, 353, 372, 391, 410, 428, 445, 462, 478, 493, 507, 520, 533, 543, 553, 562, 569, 575, 580, 583, 585, 586};
        private static readonly int[] Height47 = {1, 3, 6, 11, 17, 24, 32, 42, 53, 64, 77, 91, 106, 122, 138, 155, 173, 192, 211, 230, 249, 269, 289, 309, 329, 349, 369, 388, 407, 425, 443, 460, 477, 492, 507, 521, 534, 546, 557, 566, 575, 582, 588, 593, 596, 598, 599};
        private static readonly int[] Height48 = {1, 3, 6, 10, 16, 23, 32, 41, 52, 63, 76, 90, 104, 120, 136, 153, 171, 189, 208, 227, 246, 266, 286, 306, 326, 346, 365, 385, 404, 423, 441, 459, 476, 492, 507, 522, 536, 548, 560, 571, 580, 588, 595, 601, 606, 609, 611, 612};
        private static readonly int[] Height49 = {1, 3, 6, 10, 16, 23, 31, 40, 51, 62, 74, 88, 102, 117, 133, 150, 168, 186, 204, 223, 243, 262, 282, 302, 322, 342, 362, 381, 401, 420, 438, 456, 474, 490, 507, 522, 536, 550, 562, 573, 584, 593, 601, 608, 614, 618, 621, 623, 624};
        private static readonly int[] Height50 = {1, 3, 6, 10, 16, 22, 30, 39, 50, 61, 73, 86, 100, 115, 131, 148, 165, 183, 201, 220, 239, 259, 279, 299, 319, 339, 358, 378, 398, 417, 436, 454, 472, 489, 506, 522, 537, 551, 564, 576, 587, 598, 607, 615, 621, 627, 631, 635, 636, 637};
            
        private static readonly int[][] Heights = {Height0,
            Height1, Height2, Height3, Height4, Height5,
            Height6, Height7, Height8, Height9, Height10,
            Height11, Height12, Height13, Height14, Height15,
            Height16, Height17, Height18, Height19, Height20,
            Height21, Height22, Height23, Height24, Height25,
            Height26, Height27, Height28, Height29, Height30,
            Height31, Height32, Height33, Height34, Height35,
            Height36, Height37, Height38, Height39, Height40,
            Height41, Height42, Height43, Height44, Height45,
            Height46, Height47, Height48, Height49, Height50};
    
        public string Name => "arched";
        public BridgeType Type => BridgeType.Arched;
        public int[] ExtraArguments { get; } = {5, 10, 15, 20};
        
        public int CalculateAddedHeight(int currentSegment, int bridgeLength, int startHeight, int endHeight, int extraArgument)
        {
            int[] archHeights = Heights[bridgeLength / 2];

            int currentSegmentRelative = Math.Abs(currentSegment / 2);
            return archHeights[currentSegmentRelative];
        }
    }
}