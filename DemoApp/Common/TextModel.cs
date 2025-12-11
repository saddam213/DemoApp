using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.WPF;
using TensorStack.WPF.Services;

namespace DemoApp.Common
{
    public class TextModel : BaseModel, IModelDownload
    {
        private bool _isValid;

        public int Id { get; init; }
        public string Name { get; init; }
        public bool IsDefault { get; set; }
        public DeviceType[] SupportedDevices { get; init; }
        public TextModelType Type { get; init; }
        public string Version { get; set; }
        public int MinLength { get; init; }
        public int MaxLength { get; init; }
        public string[] Prefixes { get; init; }
        public string[] UrlPaths { get; init; }


        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetProperty(ref _isValid, value); }
        }

        public void Initialize(string modelDirectory)
        {
            var directory = System.IO.Path.Combine(modelDirectory, Name);
            var modelFiles = FileHelper.GetUrlFileMapping(UrlPaths, directory);
            if (modelFiles.Values.All(File.Exists))
            {
                IsValid = true;
                Path = directory;
            }
        }


        public async Task<bool> DownloadAsync(string modelDirectory)
        {
            var directory = System.IO.Path.Combine(modelDirectory, Name);
            if (await DialogService.DownloadAsync($"Download '{Name}' model?", UrlPaths, directory))
                Initialize(modelDirectory);

            return IsValid;
        }
    }

    public enum TextModelType
    {
        Summary = 0,
        Phi3 = 1,
        Whisper = 2,
        Supertonic = 3
    }
}
