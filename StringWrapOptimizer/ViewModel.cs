// ViewModel.cs
// 
// Copyright (c) 2016-2016 midolin limegreen All right reserved.
// 
// License:
// See LICENSE.md or README.md in solution root directory.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;

namespace KinokoBreaker
{
    public class ViewModel : BindableBase
    {
        private int _displayWidth = 1280;

        private int _selectedFontIndex;
        private readonly MainWindow _window;

        public ViewModel()
        {
            OutputText = InputText.Merge(SelectedFont)
                                  .Select(i => (object) i)
                                  .Merge(FontSize.Select(i => (object) i))
                                  .Merge(SubWidth.Select(i => (object) i))
                                  .Merge(IsBold.Select(i => (object) i))
                                  .Merge(IsItalic.Select(i => (object) i))
                                  .Merge(SmallerBreaking.Select(i => (object) i))
                                  .Merge(BreakPerChars.Select(i => (object) i))
                                  .Throttle(TimeSpan.FromMilliseconds(500))
                                  .Select(i =>
                                  {
                                      if (InputText.Value != "" && FontSize.Value > 0 && SubWidth.Value > 10)
                                      {
                                          return Model.Optimize(InputText.Value, SelectedFont.Value,
                                              FontSize.Value, IsBold.Value, IsItalic.Value, SubWidth.Value,
                                              breakPerElement: SmallerBreaking.Value,
                                              breakPerCharacter: BreakPerChars.Value);
                                      }
                                      return "";
                                  })
                                  .ToReactiveProperty();


            OutputText.Subscribe(_ => OnPropertyChanged(() => CopyResult));
            IsBold.Subscribe(_ => OnPropertyChanged(() => FontWeight));
            IsItalic.Subscribe(_ => OnPropertyChanged(() => FontStyle));
            SelectedFont.Subscribe(_ =>
            {
                if (Fonts.Contains(SelectedFont.Value) && Fonts.IndexOf(SelectedFont.Value) != SelectedFontIndex)
                        SelectedFontIndex = Fonts.IndexOf(SelectedFont.Value);
                        
            });

            BreakPerBlock = true;
        }

        public ViewModel(MainWindow window):this()
        {
            _window = window;
        }

        public bool BreakPerBlock
        {
            get { return SmallerBreaking.Value == false && BreakPerChars.Value == false; }
            set
            {
                if (value)
                {
                    SmallerBreaking.Value = false;
                    BreakPerChars.Value = false;
                }
                OnPropertyChanged();
            }
        }

        public ReactiveProperty<bool> BreakPerChars { get; set; } = new ReactiveProperty<bool>(false);

        public bool CanHyphenation { get; set; }

        public ICommand CopyResult
            => new DelegateCommand(() => Clipboard.SetText(OutputText.Value),
                () => !string.IsNullOrEmpty(OutputText?.Value));

        public int DisplayWidth
        {
            get { return _displayWidth; }
            set
            {
                _displayWidth = value;
                OnPropertyChanged();
            }
        }


        public List<string> Fonts { get; } =
            System.Windows.Media.Fonts.SystemFontFamilies.SelectMany(i => i.FamilyNames.Values,
                (j, k) => k).Distinct().OrderBy(i => i).ToList();

        public ReactiveProperty<int> FontSize { get; set; } = new ReactiveProperty<int>(32);

        public FontStyle FontStyle => IsItalic.Value ? FontStyles.Oblique : FontStyles.Normal;

        public FontWeight FontWeight => IsBold.Value ? FontWeights.Bold : FontWeights.Normal;

        public ReactiveProperty<string> InputText { get; set; } = new ReactiveProperty<string>("");

        public ReactiveProperty<bool> IsBold { get; set; } = new ReactiveProperty<bool>(false);

        public ReactiveProperty<bool> IsItalic { get; set; } = new ReactiveProperty<bool>(false);

        public bool IsOverflowStrictry { get; set; }

        public ReactiveProperty<string> OutputText { get; set; }

        public ICommand PasteFromClipboard
        {
            get
            {
                return new ActionCommand(() =>
                {
                    if (Clipboard.ContainsText())
                    {
                        InputText.Value = Clipboard.GetText();
                    }
                });
            }
        }

        public ReactiveProperty<string> SelectedFont { get; set; } = new ReactiveProperty<string>("メイリオ");

        public int SelectedFontIndex
        {
            get { return _selectedFontIndex; }
            set
            {
                if (value > 0 && value < Fonts.Count)
                {
                    _selectedFontIndex = value;
                    OnPropertyChanged();

                    var font = Fonts[value];
                    if (SelectedFont.Value != font)
                        SelectedFont.Value = font;
                }
            }
        }

        public ReactiveProperty<bool> SmallerBreaking { get; set; } = new ReactiveProperty<bool>(false);

        public ReactiveProperty<int> SubWidth { get; set; } = new ReactiveProperty<int>(1100);

        public ICommand ShowAbout
            => new ActionCommand(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory ,"..\\README.md.txt"))
                });
            });

        public ICommand Close
            => new ActionCommand(()=> _window.Close());

        public ICommand SaveResult
            => new ActionCommand(() =>
            {
                var dialog = new SaveFileDialog { CheckPathExists = true,DefaultExt = ".txt", OverwritePrompt = true};
                if (dialog.ShowDialog(_window) ?? false)
                {
                    if (File.Exists(dialog.FileName))
                    {
                        using (var writer = new StreamWriter(dialog.FileName, false))
                        {
                            writer.Write(OutputText.Value);
                        }
                    }
                    else
                    {
                        using (var writer = new StreamWriter(dialog.FileName))
                        {
                            writer.Write(OutputText.Value);
                        }
                    }
                }
            });

        public ICommand ChangeDictionary
            => new ActionCommand(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "dicrc|dicrc",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog(_window) ?? false)
                    if (File.Exists(dialog.FileName))
                        Model.DictionaryDirectory = Path.GetDirectoryName(dialog.FileName);
            });

        public ICommand SaveConfig
            => new ActionCommand(() =>
            {
                Model.SaveConfig(SubWidth.Value, SelectedFont.Value, FontSize.Value);
            });
        public ICommand LoadConfig
            => new ActionCommand(() =>
            {
                var width = 1100;
                var font = "メイリオ";
                var size = 32;
                Model.LoadConfig(ref width, ref font, ref size);
                SubWidth.Value = width;
                SelectedFont.Value = font;
                FontSize.Value = size;
            });
    }
}
