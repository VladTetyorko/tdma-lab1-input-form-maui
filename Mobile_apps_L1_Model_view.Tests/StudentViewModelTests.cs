using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests;

public class StudentViewModelTests
{
    [Fact]
    public void AddStudentCommand_CannotExecute_WhenFullNameIsEmpty()
    {
        var viewModel = new StudentViewModel();

        Assert.False(viewModel.AddStudentCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddStudentCommand_CannotExecute_WhenFullNameIsWhitespaceOrNull(string? fullName)
    {
        var viewModel = new StudentViewModel { FullName = fullName! };

        Assert.False(viewModel.AddStudentCommand.CanExecute(null));
    }

    [Fact]
    public void AddStudentCommand_CanExecute_WhenAllFieldsAreValid()
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "FIT 2-4",
            AverageScore = 4.5
        };

        Assert.True(viewModel.AddStudentCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("22")]
    [InlineData("FIT24")]
    [InlineData("FIT 2")]
    [InlineData("FIT2-4")]
    [InlineData("FIT -4")]
    [InlineData("")]
    [InlineData("   ")]
    public void AddStudentCommand_CannotExecute_WhenGroupDoesNotMatchPattern(string group)
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = group,
            AverageScore = 4.5
        };

        Assert.False(viewModel.AddStudentCommand.CanExecute(null));
        Assert.False(viewModel.IsGroupValid);
    }

    [Theory]
    [InlineData("FIT 2-4")]
    [InlineData("ФІТ 2-4")]
    [InlineData("ІПЗ 10-12")]
    public void AddStudentCommand_CanExecute_WhenGroupMatchesPattern(string group)
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = group,
            AverageScore = 4.5
        };

        Assert.True(viewModel.AddStudentCommand.CanExecute(null));
        Assert.True(viewModel.IsGroupValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(5.01)]
    [InlineData(100)]
    [InlineData(double.NaN)]
    public void AddStudentCommand_CannotExecute_WhenAverageScoreIsOutOfRange(double averageScore)
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "FIT 2-4",
            AverageScore = averageScore
        };

        Assert.False(viewModel.AddStudentCommand.CanExecute(null));
        Assert.False(viewModel.IsAverageScoreValid);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(2.5)]
    [InlineData(5.0)]
    public void AddStudentCommand_CanExecute_WhenAverageScoreIsInRange(double averageScore)
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "FIT 2-4",
            AverageScore = averageScore
        };

        Assert.True(viewModel.AddStudentCommand.CanExecute(null));
        Assert.True(viewModel.IsAverageScoreValid);
    }

    [Fact]
    public void AddStudentCommand_Execute_AddsStudentToCollection()
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "FIT 2-4",
            AverageScore = 4.5
        };

        viewModel.AddStudentCommand.Execute(null);

        var student = Assert.Single(viewModel.Students);
        Assert.Equal("Іван Петренко", student.FullName);
        Assert.Equal("FIT 2-4", student.Group);
        Assert.Equal(4.5, student.AverageScore);
    }

    [Fact]
    public void AddStudentCommand_Execute_ResetsInputFields()
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "FIT 2-4",
            AverageScore = 4.5
        };

        viewModel.AddStudentCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.FullName);
        Assert.Equal(string.Empty, viewModel.Group);
        Assert.Equal(0, viewModel.AverageScore);
    }

    [Fact]
    public void AddStudentCommand_Execute_AppendsMultipleStudentsInOrder()
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Перший",
            Group = "FIT 2-4",
            AverageScore = 4.5
        };
        viewModel.AddStudentCommand.Execute(null);

        viewModel.FullName = "Другий";
        viewModel.Group = "FIT 2-4";
        viewModel.AverageScore = 4.5;
        viewModel.AddStudentCommand.Execute(null);

        Assert.Equal(2, viewModel.Students.Count);
        Assert.Equal("Перший", viewModel.Students[0].FullName);
        Assert.Equal("Другий", viewModel.Students[1].FullName);
    }

    [Theory]
    [InlineData("123", 4.5)]
    [InlineData("FIT 2-4", -1)]
    [InlineData("FIT 2-4", 0)]
    public void AddStudentCommand_Execute_DoesNotAddStudent_WhenDataIsInvalid(string group, double averageScore)
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = group,
            AverageScore = averageScore
        };

        viewModel.AddStudentCommand.Execute(null);

        Assert.Empty(viewModel.Students);
    }

    [Theory]
    [InlineData(4.0, true)]
    [InlineData(5.0, true)]
    [InlineData(3.99, false)]
    [InlineData(0, false)]
    public void IsHighScore_ReflectsThreshold(double averageScore, bool expected)
    {
        var viewModel = new StudentViewModel { AverageScore = averageScore };

        Assert.Equal(expected, viewModel.IsHighScore);
    }

    [Fact]
    public void Greeting_IncludesFullNameAndGroup()
    {
        var viewModel = new StudentViewModel
        {
            FullName = "Іван Петренко",
            Group = "КН-101"
        };

        Assert.Equal("Студент: Іван Петренко, група КН-101", viewModel.Greeting);
    }

    [Fact]
    public void PropertyChanged_IsRaised_ForFullNameGroupAndDerivedProperties()
    {
        var viewModel = new StudentViewModel();
        var raisedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName!);

        viewModel.FullName = "Іван Петренко";

        Assert.Contains(nameof(StudentViewModel.FullName), raisedProperties);
        Assert.Contains(nameof(StudentViewModel.Greeting), raisedProperties);
    }

    [Fact]
    public void PropertyChanged_IsNotRaised_WhenValueIsUnchanged()
    {
        var viewModel = new StudentViewModel { FullName = "Іван Петренко" };
        var raiseCount = 0;
        viewModel.PropertyChanged += (_, _) => raiseCount++;

        viewModel.FullName = "Іван Петренко";

        Assert.Equal(0, raiseCount);
    }
}