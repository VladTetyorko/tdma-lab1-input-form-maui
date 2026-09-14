using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.ViewModels;

public class StudentViewModel : INotifyPropertyChanged
{
    private readonly Student _student = new();

    public StudentViewModel()
    {
        AddStudentCommand = new Command(AddStudent, CanAddStudent);
    }

    public ObservableCollection<Student> Students { get; } = new();

    public ICommand AddStudentCommand { get; }

    public string FullName
    {
        get => _student.FullName;
        set
        {
            if (_student.FullName != value)
            {
                _student.FullName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Greeting));
                ((Command)AddStudentCommand).ChangeCanExecute();
            }
        }
    }

    public string Group
    {
        get => _student.Group;
        set
        {
            if (_student.Group != value)
            {
                _student.Group = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Greeting));
            }
        }
    }

    public double AverageScore
    {
        get => _student.AverageScore;
        set
        {
            if (_student.AverageScore != value)
            {
                _student.AverageScore = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHighScore));
            }
        }
    }

    public string Greeting => $"Студент: {FullName}, група {Group}";

    public bool IsHighScore => AverageScore >= 4.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddStudent()
    {
        Students.Add(new Student
        {
            FullName = FullName,
            Group = Group,
            AverageScore = AverageScore
        });

        FullName = string.Empty;
        Group = string.Empty;
        AverageScore = 0;
    }

    private bool CanAddStudent()
    {
        return !string.IsNullOrWhiteSpace(FullName);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}