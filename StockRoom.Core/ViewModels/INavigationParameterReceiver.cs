namespace Mobile_apps_L1_Model_view.ViewModels;

// Implemented by a ViewModel that receives the parameters of a navigation (or the result of the page it opened).
// The Shell has its own IQueryAttributable, which Core cannot reference: the page implements that one and forwards here.
public interface INavigationParameterReceiver
{
    void ApplyQueryAttributes(IDictionary<string, object> query);
}
