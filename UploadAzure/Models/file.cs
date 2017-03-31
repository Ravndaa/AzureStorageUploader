using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UploadAzure.Models
{
    public class file : INotifyPropertyChanged
    {
        public string name { get; set; }
        public string path { get; set; }
        //public string status { get; set; }
        public long size { get; set; }

        public CancellationTokenSource cancel { get; set; }

        private string _status;
        public string status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChanged("status");
            }
        }

        private long _progress;
        public long progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                RaisePropertyChanged("progress");
            }
        }

        private string _message;
        public string message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged("message");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return this.path;
        }

    }
}
