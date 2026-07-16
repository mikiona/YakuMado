using YakuMado.Translation.Local;

namespace YakuMado.Translation.Local.Tests;

/// <summary>
/// 環境変数を書き換えるテストのため、[Collection]で他のテストと分離しシーケンシャル実行を保証する。
/// </summary>
[Collection("EnvironmentVariable")]
public class ArgosLocalTranslatorTests : IDisposable
{
    private readonly string? _originalEnvValue;
    private readonly string _tempPythonPath = Path.Combine(Path.GetTempPath(), $"yakumado-fake-python-{Guid.NewGuid():N}.exe");
    private readonly string _tempScriptPath = Path.Combine(Path.GetTempPath(), $"yakumado-fake-script-{Guid.NewGuid():N}.py");

    public ArgosLocalTranslatorTests()
    {
        _originalEnvValue = Environment.GetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName);
    }

    [Fact]
    public void IsAvailable_is_false_when_env_var_not_set()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, null);
        File.WriteAllText(_tempPythonPath, "");
        File.WriteAllText(_tempScriptPath, "");
        var translator = new ArgosLocalTranslator(_tempPythonPath, _tempScriptPath);

        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void IsAvailable_is_false_when_env_var_set_but_python_missing()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, "1");
        File.WriteAllText(_tempScriptPath, "");
        var translator = new ArgosLocalTranslator("C:\\nonexistent\\python.exe", _tempScriptPath);

        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void IsAvailable_is_false_when_env_var_set_but_script_missing()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, "1");
        File.WriteAllText(_tempPythonPath, "");
        var translator = new ArgosLocalTranslator(_tempPythonPath, "C:\\nonexistent\\translate_server.py");

        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void IsAvailable_is_true_when_env_var_set_and_both_files_exist()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, "1");
        File.WriteAllText(_tempPythonPath, "");
        File.WriteAllText(_tempScriptPath, "");
        var translator = new ArgosLocalTranslator(_tempPythonPath, _tempScriptPath);

        Assert.True(translator.IsAvailable);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public void IsAvailable_accepts_common_truthy_values(string envValue)
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, envValue);
        File.WriteAllText(_tempPythonPath, "");
        File.WriteAllText(_tempScriptPath, "");
        var translator = new ArgosLocalTranslator(_tempPythonPath, _tempScriptPath);

        Assert.True(translator.IsAvailable);
    }

    [Fact]
    public void IsAvailable_is_false_for_unrecognized_env_value()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, "yes-please");
        File.WriteAllText(_tempPythonPath, "");
        File.WriteAllText(_tempScriptPath, "");
        var translator = new ArgosLocalTranslator(_tempPythonPath, _tempScriptPath);

        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void SupportedLanguagePairs_contains_only_en_to_ja()
    {
        var translator = new ArgosLocalTranslator(_tempPythonPath, _tempScriptPath);

        Assert.Single(translator.SupportedLanguagePairs);
        Assert.Contains(new YakuMado.Core.Translation.LanguagePair("en", "ja"), translator.SupportedLanguagePairs);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ArgosLocalTranslator.EnabledEnvVarName, _originalEnvValue);
        if (File.Exists(_tempPythonPath)) File.Delete(_tempPythonPath);
        if (File.Exists(_tempScriptPath)) File.Delete(_tempScriptPath);
    }
}
