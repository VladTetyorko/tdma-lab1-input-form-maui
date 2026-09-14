using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests;

public class StudentTests
{
    [Fact]
    public void DefaultConstructor_InitializesEmptyStringsAndZeroScore()
    {
        var student = new Student();

        Assert.Equal(string.Empty, student.FullName);
        Assert.Equal(string.Empty, student.Group);
        Assert.Equal(0, student.AverageScore);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var student = new Student
        {
            FullName = "Іван Петренко",
            Group = "КН-101",
            AverageScore = 4.2
        };

        Assert.Equal("Іван Петренко", student.FullName);
        Assert.Equal("КН-101", student.Group);
        Assert.Equal(4.2, student.AverageScore);
    }
}