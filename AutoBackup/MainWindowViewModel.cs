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
        IObservable<FileSystemEventArgs> oFileCreated;
        IObservable<FileSystemEventArgs> oFileChanged;
        IObservable<FileSystemEventArgs> oFileDeleted;
        IObservable<FileSystemEventArgs> oFileRenamed;
        FileSystemWatcher watcher = new FileSystemWatcher();

        public MainWindowViewModel()
        {
            disposables = new CompositeDisposable();
            oFileCreated = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler =>
                {
                    FileSystemEventHandler fileSystemEventHandler = (sender, e) => handler(e);
                    return fileSystemEventHandler;
                },
                h =>
                {
                    watcher.Created += h;
                },
                h =>
                {
                    watcher.Created -= h;
                }
                );
            oFileChanged = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler =>
                {
                    FileSystemEventHandler fileSystemEventHandler = (sender, e) => handler(e);
                    return fileSystemEventHandler;
                },
                h =>
                {
                    watcher.Changed += h;
                },
                h =>
                {
                    watcher.Changed -= h;
                }
             );
            oFileDeleted = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler =>
                {
                    FileSystemEventHandler fileSystemEventHandler = (sender, e) => handler(e);
                    return fileSystemEventHandler;
                },
                h =>
                {
                    watcher.Deleted += h;
                },
                h =>
                {
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
                    watcher.IncludeSubdirectories = true;
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

        private bool _Running;
        public bool Running
        {
            get { return _Running; }
            set
            {

                UpdateProperty(ref _Running, value);
            }
        }

        private double _BackupAfterChangedDelay = 5;
        public double BackupAfterChangedDelay
        {
            get { return _BackupAfterChangedDelay; }
            set
            {

                UpdateProperty(ref _BackupAfterChangedDelay, value);
            }
        }

        private void OperationForCreate(string sFilePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sFilePath);

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
        }

        private void OperationForChange(string sFilePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sFilePath);

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
        }

        private void OperationForDelete(string sFilePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sFilePath);

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
        }

        private void OperationForRename(string sOldFullPath, string sNewFullPath)
        {

            DirectoryInfo directoryInfo = new DirectoryInfo(sNewFullPath);

            if (directoryInfo.Attributes == FileAttributes.Directory)
            {
                string sOldDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sOldFullPath.Replace(DirectoryToBackup, "").Substring(1));
                string sNewDirectory = System.IO.Path.Combine(DirectoryToStoreBackup, sNewFullPath.Replace(DirectoryToBackup, "").Substring(1));

                if (Directory.Exists(sOldDirectory))
                {
                    System.IO.Directory.Move(sOldDirectory, sNewDirectory);
                }
                else
                    System.IO.Directory.CreateDirectory(sNewDirectory);
            }
            else
            {
                string sFolderOfChangedFile = System.IO.Directory.GetParent(sNewFullPath).FullName.Replace(DirectoryToBackup, "");
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
                    System.IO.File.Move(sNewFullPath, sNewFilePath);
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
                        Running = true;
                        disposables.Add(
                            oFileCreated.Subscribe(e =>
                            {
                                Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                OperationForCreate(e.FullPath);
                            })
                            );

                        disposables.Add(
                            oFileChanged
                            .GroupBy(e => e.FullPath)
                            .Subscribe(group =>
                            {
                                group
                                .Throttle(TimeSpan.FromSeconds(BackupAfterChangedDelay))
                                .Subscribe(
                                        e =>
                                        {
                                            Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                            Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                            OperationForChange(e.FullPath);
                                        }
                                    );
                            })
                            );
                        disposables.Add(
                            oFileDeleted.Subscribe(e =>
                            {
                                Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                OperationForDelete(e.FullPath);
                            })
                            );
                        disposables.Add(
                            oFileRenamed.Subscribe(e =>
                            {
                                Console.WriteLine($"{e.ChangeType}, {e.FullPath}");
                                Console.WriteLine($"{e.ChangeType}, {DirectoryToStoreBackup}");
                                OperationForRename(((RenamedEventArgs)e).OldFullPath, e.FullPath);
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
                        Running = false;
                    });
                }

                return _CommandStop;
            }
        }
    }
}
