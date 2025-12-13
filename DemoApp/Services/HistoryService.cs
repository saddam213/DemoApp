using DemoApp.Common;
using DemoApp.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Audio;
using TensorStack.Common.Common;
using TensorStack.Image;
using TensorStack.Video;

namespace DemoApp.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly Settings _settings;
        private readonly ObservableCollection<HistoryItem> _historyCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public HistoryService(Settings settings)
        {
            _settings = settings;
            _historyCollection = [];
        }

        /// <summary>
        /// Gets the history collection.
        /// </summary>
        public ObservableCollection<HistoryItem> HistoryCollection => _historyCollection;


        /// <summary>
        /// Initialize as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            var historyFiles = Directory.EnumerateFiles(_settings.DirectoryHistory, "*.json", SearchOption.TopDirectoryOnly)
                .Select(x => new FileInfo(x))
                .OrderByDescending(x => x.CreationTimeUtc)
                .Take(_settings.MaxHistory)
                .ToList();
            foreach (var historyFile in historyFiles)
            {
                var historyItem = default(HistoryItem);
                if (historyFile.Name.StartsWith("Upscale_"))
                    historyItem = await Json.LoadAsync<UpscaleItem>(historyFile.FullName);
                if (historyFile.Name.StartsWith("Extractor_"))
                    historyItem = await Json.LoadAsync<ExtractorItem>(historyFile.FullName);
                if (historyFile.Name.StartsWith("Layer_"))
                    historyItem = await Json.LoadAsync<LayerImageItem>(historyFile.FullName);
                if (historyFile.Name.StartsWith("Diffusion_"))
                    historyItem = await Json.LoadAsync<DiffusionItem>(historyFile.FullName);
                if (historyFile.Name.StartsWith("Narrate_"))
                    historyItem = await Json.LoadAsync<NarrateItem>(historyFile.FullName);
                if (historyItem == null)
                    continue;

                historyItem.FilePath = historyFile.FullName;
                historyItem.MediaPath = Path.Combine(historyFile.DirectoryName, historyFile.Name.Replace(".json", $".{historyItem.Extension}"));
                historyItem.ThumbPath = Path.Combine(historyFile.DirectoryName, historyFile.Name.Replace(".json", ".png"));
                if (!File.Exists(historyItem.MediaPath))
                    continue;

                _historyCollection.Add(historyItem);
            }
        }


        /// <summary>
        /// Deletes the HistoryItem files.
        /// </summary>
        /// <param name="historyItem">The history item.</param>
        public Task DeleteAsync(HistoryItem historyItem)
        {
            _historyCollection.Remove(historyItem);
            FileHelper.DeleteFiles(historyItem.FilePath, historyItem.MediaPath, historyItem.ThumbPath);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Deletes the HistoryItem files with the specified MediaType.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        public async Task DeleteAsync(MediaType? mediaType)
        {
            foreach (var history in _historyCollection.Where(x => mediaType is null || x.MediaType == mediaType))
            {
                await DeleteAsync(history);
            }
            await RemoveAsync(mediaType);
        }


        /// <summary>
        /// Removes the HistoryItem from the memory list.
        /// </summary>
        /// <param name="historyItem">The history item.</param>
        public Task RemoveAsync(HistoryItem historyItem)
        {
            _historyCollection.Remove(historyItem);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Removes the HistoryItems with the specified MediaType..
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        /// <returns>Task.</returns>
        public Task RemoveAsync(MediaType? mediaType)
        {
            _historyCollection.RemoveAll(x => mediaType is null || x.MediaType == mediaType);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Add a new Image to the history timeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="image">The image.</param>
        /// <param name="history">The history.</param>
        /// <returns>A Task&lt;ImageInput&gt; representing the asynchronous operation.</returns>
        public async Task<ImageInput> AddAsync<T>(ImageInput image, T history) where T : HistoryItem
        {
            SetSavePath(history);
            await image.SaveAsync(history.MediaPath);
            await Json.SaveAsync<T>(history, history.FilePath);
            _historyCollection.Add(history);
            return image;
        }


        /// <summary>
        /// Add a new Video to the history timeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="history">The history.</param>
        /// <returns>A Task&lt;VideoInputStream&gt; representing the asynchronous operation.</returns>
        public async Task<VideoInputStream> AddAsync<T>(VideoInputStream videoStream, T history) where T : HistoryItem
        {
            SetSavePath(history);
            var newStream = await videoStream.MoveAsync(history.MediaPath);
            await videoStream.Thumbnail.SaveAsync(history.ThumbPath);
            await Json.SaveAsync<T>(history, history.FilePath);
            _historyCollection.Add(history);
            return newStream;
        }


        /// <summary>
        /// Add new Audio to the history timeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="audio">The audio.</param>
        /// <param name="history">The history.</param>
        /// <returns>A Task&lt;AudioInput&gt; representing the asynchronous operation.</returns>
        public async Task<AudioInput> AddAsync<T>(AudioInput audio, T history) where T : HistoryItem
        {
            SetSavePath(history);
            await audio.SaveAsync(history.MediaPath);
            await Json.SaveAsync<T>(history, history.FilePath);
            _historyCollection.Add(history);
            return audio;
        }


        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="history">The history.</param>
        private void SetSavePath(HistoryItem history)
        {
            var prefix = history.Source switch
            {
                View.ImageUpscale => "Upscale",
                View.ImageExtractor => "Extractor",
                View.ImageCompose => "Layer",
                View.ImageGenerate => "Diffusion",
                View.ImageDetect => "Detect",

                View.VideoUpscale => "Upscale",
                View.VideoExtractor => "Extractor",

                View.AudioNarrate => "Narrate",
                _ => throw new NotImplementedException()
            };

            var directory = Path.GetFullPath(history.Source switch
            {
                View.ImageUpscale => _settings.DirectoryHistory,
                View.ImageExtractor => _settings.DirectoryHistory,
                View.ImageCompose => _settings.DirectoryHistory,
                View.ImageGenerate => _settings.DirectoryHistory,
                View.ImageDetect => _settings.DirectoryHistory,

                View.VideoUpscale => _settings.DirectoryHistory,
                View.VideoExtractor => _settings.DirectoryHistory,

                View.AudioNarrate => _settings.DirectoryHistory,
                _ => throw new NotImplementedException()
            });

            var extension = history.MediaType switch
            {
                MediaType.Image => "png",
                MediaType.Video => "mp4",
                MediaType.Audio => "wav",
                _ => throw new NotImplementedException()
            };

            history.Extension = extension;
            var key = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            history.Id = key;
            history.FilePath = Path.Combine(directory, $"{prefix}_{key}.json");
            history.MediaPath = Path.Combine(directory, $"{prefix}_{key}.{extension}");
            history.ThumbPath = Path.Combine(directory, $"{prefix}_{key}.png");
        }
    }


    public interface IHistoryService
    {
        ObservableCollection<HistoryItem> HistoryCollection { get; }
        Task InitializeAsync();

        Task DeleteAsync(HistoryItem historyItem);
        Task RemoveAsync(HistoryItem historyItem);

        Task<ImageInput> AddAsync<T>(ImageInput image, T history) where T : HistoryItem;
        Task<VideoInputStream> AddAsync<T>(VideoInputStream videoStream, T history) where T : HistoryItem;
        Task<AudioInput> AddAsync<T>(AudioInput audio, T history) where T : HistoryItem;
    }
}
