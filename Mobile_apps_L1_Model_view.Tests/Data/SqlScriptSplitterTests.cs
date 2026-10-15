using Mobile_apps_L1_Model_view.Data.Migrations;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class SqlScriptSplitterTests
{
    [Fact]
    public void SemicolonInsideAStringLiteral_DoesNotSplit()
    {
        var statements = SqlScriptSplitter.Split("INSERT INTO T VALUES ('a;b'); SELECT 1;");

        Assert.Equal(["INSERT INTO T VALUES ('a;b')", "SELECT 1"], statements);
    }

    [Fact]
    public void EscapedQuoteInsideAStringLiteral_KeepsTheLiteralOpen()
    {
        var statements = SqlScriptSplitter.Split("SELECT 'it''s; fine'; SELECT 2;");

        Assert.Equal(["SELECT 'it''s; fine'", "SELECT 2"], statements);
    }

    [Fact]
    public void DoubleDashInsideAStringLiteral_IsNotAComment()
    {
        var statements = SqlScriptSplitter.Split("SELECT '--not a comment'; SELECT 2;");

        Assert.Equal(["SELECT '--not a comment'", "SELECT 2"], statements);
    }

    [Fact]
    public void CommentsWithSemicolons_AreIgnored()
    {
        var script = "-- a; comment\nCREATE TABLE A (x INT); -- trailing; comment\nSELECT 1;";

        var statements = SqlScriptSplitter.Split(script);

        Assert.Equal(["CREATE TABLE A (x INT)", "SELECT 1"], statements);
    }

    [Fact]
    public void BlankStatements_AreDropped()
    {
        var statements = SqlScriptSplitter.Split(";;  ;\n SELECT 1;;");

        Assert.Equal(["SELECT 1"], statements);
    }

    [Fact]
    public void TrailingStatementWithoutSemicolon_IsKept()
    {
        var statements = SqlScriptSplitter.Split("SELECT 1; SELECT 2");

        Assert.Equal(["SELECT 1", "SELECT 2"], statements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    [InlineData("-- only a comment\n")]
    public void EmptyOrCommentOnlyScript_HasNoStatements(string script)
    {
        Assert.Empty(SqlScriptSplitter.Split(script));
    }

    [Fact]
    public void MultiLineStatement_KeepsItsLineBreaks()
    {
        var statements = SqlScriptSplitter.Split("CREATE TABLE A (\n  x INT\n);");

        Assert.Equal(["CREATE TABLE A (\n  x INT\n)"], statements);
    }
}
