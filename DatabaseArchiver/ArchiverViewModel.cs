using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Farrellcrafts.DatabaseArchiver
{
    public class ArchiverViewModel
    {
        private ICommand _archiveCommand;
        
        
        public ArchiverViewModel() {
            _archiveCommand = new ClickCommand(ArchiveDatabase);
        }
        public ICommand ArchiveCommand{
            get
            {
                return _archiveCommand;
            }
        }

        public string Server { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public SecureString Password { get; set; }

        public void ArchiveDatabase() {
            Password.MakeReadOnly();
            DatabaseArchiver archiver = new DatabaseArchiver(Server, Database, User, Password);
        }

        class ClickCommand : ICommand
        {
            Action action;
            public ClickCommand(Action action) {
                this.action = action;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            #pragma warning disable 67
            public event EventHandler CanExecuteChanged { add { } remove { } }
            #pragma warning restore 67

            public void Execute(object parameter)
            {
                action();
            }
        }
    }
}
