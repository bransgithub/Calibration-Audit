﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using NationalInstruments.SystemConfiguration;

namespace NationalInstruments.Examples.CalibrationAudit
{
    class CalibrationAuditWorker : INotifyPropertyChanged
    {
        private bool canBeginRunAudit;
        private bool shouldShowNetworkDevices;
        private List<HardwareViewModel> allHardwareResources;

        public CalibrationAuditWorker()
        {
            CanBeginRunAudit = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanBeginRunAudit
        {
            get { return canBeginRunAudit; }
            set
            {
                if (canBeginRunAudit != value)
                {
                    canBeginRunAudit = value;
                    NotifyPropertyChanged("CanBeginRunAudit");
                }
            }
        }

        public string Target
        {
            get;
            set;
        }

        public string Username
        {
            get;
            set;
        }

        public IEnumerable<HardwareViewModel> FilteredHardwareResources
        {
            get
            {
                if (AllHardwareResources == null)
                {
                    return Enumerable.Empty<HardwareViewModel>();
                }
                return from resource in AllHardwareResources
                       where !(resource.NumberOfExperts == 1 && resource.Expert0ProgrammaticName.Equals("network"))
                       select resource;
            }
        }

        private List<HardwareViewModel> AllHardwareResources
        {
            get { return allHardwareResources; }
            set
            {
                if (allHardwareResources != value)
                {
                    allHardwareResources = value;
                    NotifyPropertyChanged("FilteredHardwareResources");
                }
            }
        }

        public void StartRunAudit(string password)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(
                delegate(object o, DoWorkEventArgs args)
                {
                    CanBeginRunAudit = false;
                    try
                    {
                        // Because the view does not allow modifying resources, there isn't a need to keep
                        // the raw HardwareResourceBase objects after creating the view models.
                        AllHardwareResources = null;
                        var session = new SystemConfiguration.SystemConfiguration(Target, Username, password);

                        SystemConfiguration.Filter filter = new SystemConfiguration.Filter(session); 
                        filter.IsDevice = true;
                        filter.SupportsCalibration = true;
                        filter.IsPresent = SystemConfiguration.IsPresentType.Present;
                        filter.IsSimulated = false;

                        ResourceCollection rawResources = session.FindHardware(filter); 
                        AllHardwareResources =
                            (from resource in rawResources //issue is that this returns hardwareresourcebase but in hardwareviewmodel we have productresource class
                             select new HardwareViewModel(resource)).ToList();
                    }
                    catch (SystemConfigurationException ex)
                    {
                        string errorMessage = string.Format("Find Hardware threw a System Configuration Exception.\n\nErrorCode: {0:X}\n{1}", ex.ErrorCode, ex.Message);
                        MessageBox.Show(errorMessage, "System Configuration Exception");
                    }
                    finally
                    {
                        CanBeginRunAudit = true;
                    }
                }
            );
            worker.RunWorkerAsync();
        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
