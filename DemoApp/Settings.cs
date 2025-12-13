using DemoApp.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Providers;
using TensorStack.WPF;

namespace DemoApp
{
    public class Settings : IUIConfiguration
    {
        [JsonIgnore]
        public Device DefaultDevice { get; set; }
        public int ReadBuffer { get; set; } = 32;
        public int WriteBuffer { get; set; } = 32;
        public string VideoCodec { get; set; } = "mp4v";
        public string DirectoryTemp { get; set; }
        public string DirectoryModel { get; set; }
        public string DirectoryHistory { get; set; }
        public int MaxHistory { get; set; } = 500;
        public IReadOnlyList<Device> Devices { get; set; }


        public ObservableCollection<UpscaleModel> UpscaleModels { get; set; }
        public ObservableCollection<ExtractorModel> ExtractorModels { get; set; }
        public ObservableCollection<DiffusionModel> DiffusionModels { get; set; }
        public ObservableCollection<DiffusionControlNetModel> DiffusionControlNetModels { get; set; }
        public ObservableCollection<DetectModel> DetectModels { get; set; }
        public ObservableCollection<TextModel> TextModels { get; set; }
        public ObservableCollection<TranscribeModel> TranscribeModels { get; set; }
        public ObservableCollection<NarrateModel> NarrateModels { get; set; }


        public void Initialize()
        {
            // DirectoryTemp = Path.GetFullPath(DirectoryTemp);
            // DirectoryModel = Path.GetFullPath(DirectoryModel);
            // DirectoryHistory = Path.GetFullPath(DirectoryHistory);

            Directory.CreateDirectory(DirectoryTemp);
            Directory.CreateDirectory(DirectoryModel);
            Directory.CreateDirectory(DirectoryHistory);

            Provider.Initialize();
            Devices = Provider.GetDevices();
            DefaultDevice = Provider.GetDevice();

            ScanModels();
        }


        private void ScanModels()
        {
            var upscaleDirectory = Path.Combine(DirectoryModel, "Upscale");
            foreach (var upscaleModel in UpscaleModels)
                upscaleModel.Initialize(upscaleDirectory);

            var extractorDirectory = Path.Combine(DirectoryModel, "Extractor");
            foreach (var extractorModel in ExtractorModels)
                extractorModel.Initialize(extractorDirectory);

            var detectDirectory = Path.Combine(DirectoryModel, "Detect");
            foreach (var detectModel in DetectModels)
                detectModel.Initialize(detectDirectory);

            var textDirectory = Path.Combine(DirectoryModel, "Text");
            foreach (var textModel in TextModels)
                textModel.Initialize(textDirectory);

            var transcribeDirectory = Path.Combine(DirectoryModel, "Transcribe");
            foreach (var transcribeModel in TranscribeModels)
                transcribeModel.Initialize(transcribeDirectory);

            var narrateDirectory = Path.Combine(DirectoryModel, "Narrate");
            foreach (var narrateModel in NarrateModels)
                narrateModel.Initialize(narrateDirectory);

            var diffusionDirectory = Path.Combine(DirectoryModel, "Diffusion");
            foreach (var diffusionModel in DiffusionModels)
                diffusionModel.Initialize(diffusionDirectory);

            var controlNetDirectory = Path.Combine(DirectoryModel, "Control");
            foreach (var diffusionControlNetModels in DiffusionControlNetModels)
                diffusionControlNetModels.Initialize(controlNetDirectory);
        }




        public void SetDefault(UpscaleModel model)
        {
            foreach (var existing in UpscaleModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(ExtractorModel model)
        {
            foreach (var existing in ExtractorModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(DiffusionModel model)
        {
            foreach (var existing in DiffusionModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(DetectModel model)
        {
            foreach (var existing in DetectModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(TextModel model)
        {
            foreach (var existing in TextModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(TranscribeModel model)
        {
            foreach (var existing in TextModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }

        public void SetDefault(NarrateModel model)
        {
            foreach (var existing in NarrateModels)
            {
                existing.IsDefault = false;
            }
            model.IsDefault = true;
        }
    }
}