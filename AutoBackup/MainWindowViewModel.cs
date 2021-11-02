using MYLibrary.Bindings;
using MYLibrary.Bindings.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    public class MainWindowViewModel : BindableBase
    {
        CompositeDisposable disposables;
        IObservable<FileSystemEventArgs> oFileContentChanged;
        IObservable<FileSystemEventArgs> oFileRenamed;
        FileSystemWatcher watcher = new FileSystemWatcher();

        public MainWindowViewModel()
        {
            disposables = new CompositeDisposable();
            oFileContentChanged = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler =>
                {
                    FileSystemEventHandler fileSystemEventHandler = (sender, e) => handler(e);
                    return fileSystemEventHandler;
                },
                h =>
                {
                    watcher.Changed += h;
                    watcher.Created += h;
                    watcher.Deleted += h;
                },
                 h =>
                 {
                     watcher.Changed -= h;
                     watcher.Created -= h;
                     watcher.Deleted -= h;
                 }
                );
            oFileRenamed = Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                handler =>
                {
                    RenamedEventHandler renamedEventHandler = (sender, e) => handler(e);
                    return renamedEventHandler;
                },
                h =>
                {
                    watcher.Renamed += h;
                },
                 h =>
                 {
                     watcher.Renamed -= h;
                 }
                );
        }

        public ObservableCollection<string> BackupTypeList { get; set; } = new ObservableCollection<string>();

        private string _DirectoryToBackup;

        public string DirectoryToBackup
        {
            get { return _DirectoryToBackup; }
            set
            {
                if (System.IO.Directory.Exists(value))
                {
                    watcher.Path = value;
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                         | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watcher.EnableRaisingEvents = true;
                }
                else
                {
                    return;
                }
                UpdateProperty(ref _DirectoryToBackup, value);
            }
        }

        private string _DirectoryToStoreBackup;

        public string DirectoryToStoreBackup
        {
            get { return _DirectoryToStoreBackup; }
            set
            {

                UpdateProperty(ref _DirectoryToStoreBackup, value);
            }
        }

        /// <summary>
        /// 每种修改都可以通过拷贝来备份
        /// </summary>
        /// <param name="sFilePath"></param>
        private void OperateFile(string sFilePath, WatcherChangeTypes watcherChangeTypes, string sOldFullPath = "")
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sFilePath);

            switch (watcherChangeTypes)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                    if (directoryInfo.Attributes == FileAttributes.Directory)
                    {
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sFilePath.Replace(DirectoryToBackup, "").Substring(1));
                        System.IO.Directory.CreateDirectory(sNewDirectory);
                    }
                    else
                    {
                        string sFolderOfChangedFile = System.IO.Directory.GetParent(sFilePath).FullName.Replace(DirectoryToBackup, "");
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup,
                                sFolderOfChangedFile.Length > 0 ? sFolderOfChangedFile.Substring(1) : sFolderOfChangedFile);
                        System.IO.Directory.CreateDirectory(sNewDirectory);
                        System.IO.File.Copy(sFilePath, sNewDirectory + "\\" + directoryInfo.Name);
                    }
                    break;
                case WatcherChangeTypes.Deleted:
                    if (directoryInfo.Attributes == FileAttributes.Directory)
                    {
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sFilePath.Replace(DirectoryToBackup, "").Substring(1));
                        System.IO.Directory.Delete(sNewDirectory);
                    }
                    else
                    {
                        string sFolderOfChangedFile = System.IO.Directory.GetParent(sFilePath).FullName.Replace(DirectoryToBackup, "");
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup,
                                sFolderOfChangedFile.Length > 0 ? sFolderOfChangedFile.Substring(1) : sFolderOfChangedFile);
                        System.IO.Directory.CreateDirectory(sNewDirectory);
                        string sBackupedFilePath;
                        sBackupedFilePath = sNewDirectory + "\\" + directoryInfo.Name;
                        if (System.IO.File.Exists(sBackupedFilePath))
                        {
                            System.IO.File.Delete(sBackupedFilePath);
                        }
                    }
                    break;
                case WatcherChangeTypes.Renamed:
                    if (directoryInfo.Attributes == FileAttributes.Directory)
                    {
                        string sOldDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sOldFullPath.Replace(DirectoryToBackup, "").Substring(1));
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sFilePath.Replace(DirectoryToBackup, "").Substring(1));

                        if (Directory.Exists(sOldDirectory))
                        {
                            System.IO.Directory.Move(sOldDirectory, sNewDirectory);
                        }
                        else
                            System.IO.Directory.CreateDirectory(sNewDirectory);
                    }
                    else
                    {
                        string sFolderOfChangedFile = System.IO.Directory.GetParent(sFilePath).FullName.Replace(DirectoryToBackup, "");
                        string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup,
                                sFolderOfChangedFile.Length > 0 ? sFolderOfChangedFile.Substring(1) : sFolderOfChangedFile);
                        System.IO.Directory.CreateDirectory(sNewDirectory);

                        string sBackupedFilePath;
                        string sNewFilePath;
                        sNewFilePath = sNewDirectory + "\\" + directoryInfo.Name;
                        sBackupedFilePath = sNewDirectory + "\\" + System.IO.Path.GetFileName(sOldFullPath);
                        if (File.Exists(sBackupedFilePath))
                        {
                            System.IO.File.Move(sBackupedFilePath, sNewFilePath);
                        }
                        else
                            System.IO.File.Move(sFilePath, sNewFilePath);

                    }
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    break;
            }

        }

        private RelayCommand _CommandStart;
        public RelayCommand CommandStart
        {
            get
            {
                if (_CommandStart == null)
                {
                    _CommandStart = new RelayCommand((o) =>
                    {
                        disposables.Add(
                            oFileContentChanged.Subscribe(e =>
                            {
                                Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                OperateFile(e.FullPath, e.ChangeType);
                            })
                            );

                        disposables.Add(
                            oFileRenamed.Subscribe(e =>
                            {
                                Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                OperateFile(e.FullPath, e.ChangeType, ((RenamedEventArgs)e).OldFullPath);
                            })
                            );
                    });
                }

                return _CommandStart;
            }
        }

        private RelayCommand _CommandStop;
        public RelayCommand CommandStop
        {
            get
            {
                if (_CommandStop == null)
                {
                    _CommandStop = new RelayCommand((o) =>
                    {
                        disposables.Dispose();
                    });
                }

                return _CommandStop;
            }
        }
    }
}
