using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Audio;
using TensorStack.TextGeneration.Common;
using TensorStack.TextGeneration.Pipelines.Whisper;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for AudioWhisperView.xaml
    /// </summary>
    public partial class AudioWhisperView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private int _topK = 50;
        private int _beams = 4;
        private int _seed = -1;
        private float _topP = 0.9f;
        private float _temperature = 1f;
        private float _lengthPenalty = 1f;
        private int _diversityLength = 512;
        private int _minLength = 20;
        private int _maxLength = 512;
        private EarlyStopping _earlyStopping = EarlyStopping.None;
        private bool _isMultipleResult;
        private int _selectedBeam;
        private AudioInput _audioInput;
        private TaskType _selectedTask = TaskType.Transcribe;
        private LanguageType _selectedLanguage = LanguageType.EN;

        public AudioWhisperView(Settings settings, NavigationService navigationService, IHistoryService historyService, IWhisperService whisperService)
            : base(settings, navigationService, historyService)
        {

            TextService = whisperService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            Prefixes = new ObservableCollection<string>();
            Results = new ObservableCollection<WhisperResult>();
            InitializeComponent();
        }

        public override int Id => (int)View.AudioDefault;
        public IWhisperService TextService { get; }
        public AsyncRelayCommand ExecuteCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public ObservableCollection<string> Prefixes { get; }
        public ObservableCollection<WhisperResult> Results { get; }
        public WhisperResult Result => Results?.FirstOrDefault();

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
        }
        public int TopK
        {
            get { return _topK; }
            set { SetProperty(ref _topK, value); }
        }

        public int Beams
        {
            get { return _beams; }
            set { SetProperty(ref _beams, value); }
        }

        public int Seed
        {
            get { return _seed; }
            set { SetProperty(ref _seed, value); }
        }

        public float TopP
        {
            get { return _topP; }
            set { SetProperty(ref _topP, value); }
        }

        public float Temperature
        {
            get { return _temperature; }
            set { SetProperty(ref _temperature, value); }
        }

        public float LengthPenalty
        {
            get { return _lengthPenalty; }
            set { SetProperty(ref _lengthPenalty, value); }
        }

        public int DiversityLength
        {
            get { return _diversityLength; }
            set { SetProperty(ref _diversityLength, value); }
        }

        public int MinLength
        {
            get { return _minLength; }
            set { SetProperty(ref _minLength, value); }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set { SetProperty(ref _maxLength, value); }
        }

        public EarlyStopping EarlyStopping
        {
            get { return _earlyStopping; }
            set { SetProperty(ref _earlyStopping, value); }
        }

        public bool IsMultipleResult
        {
            get { return _isMultipleResult; }
            set { SetProperty(ref _isMultipleResult, value); }
        }

        public int SelectedBeam
        {
            get { return _selectedBeam; }
            set { SetProperty(ref _selectedBeam, value); }
        }

        public AudioInput AudioInput
        {
            get { return _audioInput; }
            set { SetProperty(ref _audioInput, value); }
        }

        public TaskType SelectedTask
        {
            get { return _selectedTask; }
            set { SetProperty(ref _selectedTask, value); }
        }

        public LanguageType SelectedLanguage
        {
            get { return _selectedLanguage; }
            set { SetProperty(ref _selectedLanguage, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = TextService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate("Generating Results...");
            try
            {
                // Run Summary
                var results = await TextService.ExecuteAsync(new WhisperRequest
                {
                    Beams = _beams,
                    TopK = _topK,
                    Seed = _seed,
                    TopP = _topP,
                    Temperature = _temperature,
                    LengthPenalty = _lengthPenalty,
                    MinLength = _minLength,
                    MaxLength = _maxLength,
                    NoRepeatNgramSize = 4,
                    DiversityLength = _diversityLength,
                    EarlyStopping = _earlyStopping,
                    AudioInput = _audioInput,
                    Language = _selectedLanguage,
                    Task = _selectedTask
                });

                Results.Clear();
                foreach (var result in results)
                {
                    Results.Add(new WhisperResult($"Beam {result.Beam}", result.Result, result.PenaltyScore));
                }
                SelectedBeam = 0;

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
            NotifyPropertyChanged(nameof(Result));
        }


        private bool CanExecute()
        {
            return _audioInput is not null && TextService.IsLoaded && !TextService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await TextService.CancelAsync();
        }


        private bool CanCancel()
        {
            return TextService.CanCancel;
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.TextModel == null)
            {
                await TextService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await TextService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.TextModel);

            var model = _currentPipeline.TextModel;
            MinLength = model.MinLength;
            MaxLength = model.MaxLength;
            DiversityLength = model.MinLength;
            //if (model.Prefixes != null)
            //{
            //    foreach (var prefix in model.Prefixes)
            //    {
            //        Prefixes.Add(prefix);
            //    }
            //    SelectedPrefix = Prefixes.FirstOrDefault();
            //}

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }

    }

    public record WhisperResult(string Header, string Content, float Score);
}