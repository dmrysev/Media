namespace Media.UI.Core;
  
using System.ComponentModel;

public abstract class ViewModelBase :  INotifyPropertyChanged {
  public event PropertyChangedEventHandler PropertyChanged;

  public virtual void OnPropertyChanged(string propertyName)
  {
      var propertyChanged = PropertyChanged;
      if (propertyChanged != null)
      {
          propertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
  }
}