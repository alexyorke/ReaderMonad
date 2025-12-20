using ReaderMonad;

namespace ReaderMonad.Tests;

public sealed class ReaderTests
{
    private sealed record Env(int Multiplier, string Name);

    [Fact]
    public void Pure_returns_value_and_ignores_environment()
    {
        var r = Reader<Env, int>.Pure(123);

        Assert.Equal(123, r.Run(new Env(Multiplier: 0, Name: "a")));
        Assert.Equal(123, r.Run(new Env(Multiplier: 999, Name: "b")));
    }

    [Fact]
    public void Map_transforms_the_result()
    {
        var r =
            Reader.From<Env, int>(env => env.Multiplier)
                  .Map(x => x * 2);

        Assert.Equal(10, r.Run(new Env(Multiplier: 5, Name: "x")));
    }

    [Fact]
    public void Ask_returns_the_current_environment_instance()
    {
        var env = new Env(Multiplier: 7, Name: "prod");
        var r = Reader<Env, Env>.Ask();

        Assert.Same(env, r.Run(env));
    }

    [Fact]
    public void Bind_runs_both_computations_under_the_same_environment()
    {
        var r =
            Reader.From<Env, int>(env => env.Multiplier)
                  .Bind(x => Reader.From<Env, int>(env => x + env.Multiplier));

        Assert.Equal(6, r.Run(new Env(Multiplier: 3, Name: "x")));
    }

    [Fact]
    public void Local_transforms_environment_for_the_subcomputation_only()
    {
        var getMult = Reader.From<Env, int>(env => env.Multiplier);

        var r =
            from original in getMult
            from transformed in getMult.Local(e => e with { Multiplier = e.Multiplier + 10 })
            select (original, transformed);

        Assert.Equal((2, 12), r.Run(new Env(Multiplier: 2, Name: "x")));
    }

    [Fact]
    public void Linq_query_syntax_uses_SelectMany_and_Select_correctly()
    {
        var r =
            from env in Reader<Env, Env>.Ask()
            from m in Reader.From<Env, int>(e => e.Multiplier)
            select $"{env.Name}:{m}";

        Assert.Equal("dev:4", r.Run(new Env(Multiplier: 4, Name: "dev")));
    }
}