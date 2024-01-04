using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nodus.Core.Extensions;

namespace Nodus.Core.Effects;

public interface IShaderUniformFactory
{
    IShaderUniform? Create(string uniformString);
}

public interface IShaderUniformParser
{
    IShaderUniform Parse(string uniformString);
    bool CanParse(string uniformString);
}

public readonly struct GenericShaderUniformParser : IShaderUniformParser
{
    private readonly string pattern;
    private readonly Func<string, IShaderUniform> factory;
    
    public GenericShaderUniformParser(string pattern, Func<string, IShaderUniform> factory)
    {
        this.pattern = pattern;
        this.factory = factory;
    }
    
    public IShaderUniform Parse(string uniformString)
    {
        return factory.Invoke(uniformString);
    }

    public bool CanParse(string uniformString)
    {
        return Regex.IsMatch(uniformString, pattern);
    }
}

public class ShaderUniformFactory : IShaderUniformFactory
{
    private readonly ISet<IShaderUniformParser> parsers;

    public ShaderUniformFactory(IEnumerable<IShaderUniformParser>? parsers = null)
    {
        this.parsers = new HashSet<IShaderUniformParser>(parsers ?? GetDefaultParsers());
    }
    
    public IShaderUniform? Create(string uniformString)
    {
        return parsers.FirstOrDefault(x => x.CanParse(uniformString))?.Parse(uniformString);
    }

    private IEnumerable<IShaderUniformParser> GetDefaultParsers()
    {
        yield return new GenericShaderUniformParser(@"[^\s=]+=[+-]?(\d+(\.\d*)?|\.\d+)", s =>
        {
            var spliced = s.Split('=');
            return new ConstantUniform(spliced[0], new []{float.Parse(spliced[1])});
        });
        
        yield return new GenericShaderUniformParser(@"[^\s=]+\=\[(\s*[+-]?(\d+(\.\d*)?|\.\d+)(\s*,\s*[+-]?(\d+(\.\d*)?|\.\d+))*\s*)\]", s =>
        {
            var spliced = s.Split('=');
            var splicedValue = spliced[1].Substring(1, spliced[1].Length - 2);
            return new ConstantUniform(spliced[0], splicedValue.Split(',').Select(float.Parse).ToArray());
        });
    }
}