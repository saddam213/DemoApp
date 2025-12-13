using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Audio;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for AudioNarrateView.xaml
    /// </summary>
    public partial class AudioNarrateView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private string _selectedVoice;
        private AudioInput _audioResult;
        private int _steps = 10;
        private float _speed = 1f;
        private string _inputText;
        private int _seed;

        public AudioNarrateView(Settings settings, NavigationService navigationService, IHistoryService historyService, INarrateService narrateService)
            : base(settings, navigationService, historyService)
        {
            NarrateService = narrateService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            Voices = new ObservableCollection<string>();
            InitializeComponent();
        }

        public override int Id => (int)View.AudioNarrate;
        public INarrateService NarrateService { get; }
        public AsyncRelayCommand ExecuteCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public ObservableCollection<string> Voices { get; }


        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
        }

        public string SelectedVoice
        {
            get { return _selectedVoice; }
            set { SetProperty(ref _selectedVoice, value); }
        }

        public string InputText
        {
            get { return _inputText; }
            set { SetProperty(ref _inputText, value); }
        }

        public float Speed
        {
            get { return _speed; }
            set { SetProperty(ref _speed, value); }
        }

        public int Steps
        {
            get { return _steps; }
            set { SetProperty(ref _steps, value); }
        }

        public int Seed
        {
            get { return _seed; }
            set { SetProperty(ref _seed, value); }
        }

        public AudioInput AudioResult
        {
            get { return _audioResult; }
            set { SetProperty(ref _audioResult, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = NarrateService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate("Generating Results...");
            try
            {
                // Generate Result
                var result = await NarrateService.ExecuteAsync(new NarrateRequest
                {
                    InputText = _inputText,
                    Speed = _speed,
                    Steps = _steps,
                    Seed = _seed,
                    VoiceStyle = _selectedVoice
                });

                // Save History
                var resultAudio = new AudioInput(result);
                AudioResult = await HistoryService.AddAsync(resultAudio, new NarrateItem
                {
                    Source = View.AudioNarrate,
                    MediaType = MediaType.Audio,
                    Model = _currentPipeline.NarrateModel.Name,
                    Voice = _selectedVoice,
                    Seed = _seed,
                    Speed = _speed,
                    Steps = _steps,
                    Channels = resultAudio.Channels,
                    SampleRate = resultAudio.SampleRate,
                    Duration = resultAudio.Duration,
                    InputText = _inputText,
                    Timestamp = DateTime.UtcNow
                });

                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Complete: {Stopwatch.GetElapsedTime(timestamp)}");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Cancelled: {Stopwatch.GetElapsedTime(timestamp)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Error: {Stopwatch.GetElapsedTime(timestamp)}");
                await DialogService.ShowErrorAsync("ExecuteAsync", ex.Message);
            }

            Progress.Clear();
        }


        private bool CanExecute()
        {
            return !string.IsNullOrEmpty(_inputText) && NarrateService.IsLoaded && !NarrateService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await NarrateService.CancelAsync();
        }


        private bool CanCancel()
        {
            return NarrateService.CanCancel;
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.NarrateModel == null)
            {
                await NarrateService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await NarrateService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.NarrateModel);

            var model = _currentPipeline.NarrateModel;


            if (model.Voices != null)
            {
                foreach (var prefix in model.Voices)
                {
                    Voices.Add(prefix);
                }
                SelectedVoice = Voices.FirstOrDefault();
            }

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }

    }
}