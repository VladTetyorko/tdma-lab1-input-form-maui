using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.ViewModels;

public class StudentViewModel : INotifyPropertyChanged
{
    // "FIT 2-4": faculty code, then year-group as "2-4"
    private static readonly Regex GroupPattern =
        new(@"^[A-Za-zА-Яа-яІіЇїЄєҐґ]{2,10} \d{1,2}-\d{1,2}$", RegexOptions.Compiled);

    private const double MinAverageScore = 0.0;
    private const double MaxAverageScore = 5.0;

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
                OnPropertyChanged(nameof(IsGroupValid));
                ((Command)AddStudentCommand).ChangeCanExecute();
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
                OnPropertyChanged(nameof(IsAverageScoreValid));
                ((Command)AddStudentCommand).ChangeCanExecute();
            }
        }
    }

    public string Greeting => $"Студент: {FullName}, група {Group}";

    public bool IsHighScore => AverageScore >= 4.0;

    public bool IsGroupValid => GroupPattern.IsMatch(Group);

    public bool IsAverageScoreValid => AverageScore > MinAverageScore && AverageScore <= MaxAverageScore;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddStudent()
    {
        if (!CanAddStudent())
        {
            return;
        }

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
        return !string.IsNullOrWhiteSpace(FullName) && IsGroupValid && IsAverageScoreValid;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}