using System.Collections.Generic;

namespace DemoApp.Views
{
    public enum View
    {
        Settings = 0,
        History = 1,

        TextSummary = 100,

        ImageUpscale = 200,
        ImageExtractor = 201,
        ImageCompose = 202,
        ImageGenerate = 203,
        ImageTransform = 204,
        ImageDetect = 205,

        VideoUpscale = 300,
        VideoExtractor = 301,
        VideoInterpolation = 302,

        AudioTranscribe = 400,
        AudioNarrate = 401,
    }


    public enum ViewCategory
    {
        Text = 0,
        Image = 1,
        Video = 2,
        Audio = 3,

        Other = 100
    }

    public static class ViewManager
    {

        private static readonly Dictionary<ViewCategory, View> CurrentViewMap = new Dictionary<ViewCategory, View>
        {
            {ViewCategory.Other, View.Settings },
            {ViewCategory.Text, View.TextSummary },
            {ViewCategory.Image, View.ImageExtractor },
            {ViewCategory.Video, View.VideoUpscale },
            {ViewCategory.Audio, View.AudioTranscribe }
        };


        private static readonly Dictionary<View, ViewCategory> ViewCategoryMap = new Dictionary<View, ViewCategory>
        {
            // General
            { View.Settings, ViewCategory.Other  },
            { View.History, ViewCategory.Other  },

            // Text
            { View.TextSummary, ViewCategory.Text  },

            // Image
            { View.ImageUpscale, ViewCategory.Image  },
            { View.ImageExtractor, ViewCategory.Image  },
            { View.ImageCompose, ViewCategory.Image  },
            { View.ImageGenerate, ViewCategory.Image  },
            { View.ImageTransform, ViewCategory.Image  },
            { View.ImageDetect, ViewCategory.Image  },

            // Video
            { View.VideoUpscale, ViewCategory.Video  },
            { View.VideoExtractor, ViewCategory.Video  },
            { View.VideoInterpolation, ViewCategory.Video  },

            // Audio
            { View.AudioTranscribe, ViewCategory.Audio  },
            { View.AudioNarrate, ViewCategory.Audio  }
        };


        internal static View GetCurrentView(ViewCategory category)
        {
            return CurrentViewMap[category];
        }


        internal static ViewCategory SetCurrentView(View view)
        {
            var category = ViewCategoryMap[view];
            CurrentViewMap[category] = view;
            return category;
        }
    }
}
