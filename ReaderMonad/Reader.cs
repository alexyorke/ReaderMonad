namespace ReaderMonad;

// ---------- Core: Reader monad ----------

public sealed class Reader<TEnv, T>
{
    private readonly Func<TEnv, T> _run;

    public Reader(Func<TEnv, T> run)
    {
        _run = run;
    }

    public T Run(TEnv env)
    {
        return _run(env);
    }

    // Unit / Pure: lifts a value into Reader (ignores the environment)
    public static Reader<TEnv, T> Pure(T value)
    {
        return new Reader<TEnv, T>(
            _ =>
            {
                return value;
            }
        );
    }

    // Functor: Map transforms the eventual result
    public Reader<TEnv, TResult> Map<TResult>(Func<T, TResult> f)
    {
        return new Reader<TEnv, TResult>(
            env =>
            {
                T a = _run(env);
                TResult b = f(a);
                return b;
            }
        );
    }

    // LINQ Select is just Map
    public Reader<TEnv, TResult> Select<TResult>(Func<T, TResult> f)
    {
        return Map(f);
    }

    // Monad: Bind sequences computations that depend on the same environment
    public Reader<TEnv, TResult> Bind<TResult>(Func<T, Reader<TEnv, TResult>> f)
    {
        return new Reader<TEnv, TResult>(
            env =>
            {
                T a = _run(env); // run first computation
                Reader<TEnv, TResult> next = f(a); // choose next computation based on result
                TResult b = next.Run(env); // run next computation under the same env
                return b;
            }
        );
    }

    // Enables LINQ query syntax: from x in ... from y in ... select ...
    public Reader<TEnv, TResult> SelectMany<TMid, TResult>(
        Func<T, Reader<TEnv, TMid>> bind,
        Func<T, TMid, TResult> project)
    {
        return Bind(
            a =>
            {
                Reader<TEnv, TMid> rb = bind(a);

                return rb.Map(
                    b =>
                    {
                        return project(a, b);
                    }
                );
            }
        );
    }

    // Ask: returns the current environment
    public static Reader<TEnv, TEnv> Ask()
    {
        return new Reader<TEnv, TEnv>(
            env =>
            {
                return env;
            }
        );
    }

    // Local: runs this Reader under a transformed environment
    public Reader<TEnv, T> Local(Func<TEnv, TEnv> transform)
    {
        return new Reader<TEnv, T>(
            env =>
            {
                TEnv env2 = transform(env);
                T result = _run(env2);
                return result;
            }
        );
    }
}

// Static helpers for ergonomics (optional)
public static class Reader
{
    public static Reader<TEnv, T> Unit<TEnv, T>(T value)
    {
        return Reader<TEnv, T>.Pure(value);
    }

    public static Reader<TEnv, TEnv> Ask<TEnv>()
    {
        return Reader<TEnv, TEnv>.Ask();
    }

    public static Reader<TEnv, T> From<TEnv, T>(Func<TEnv, T> f)
    {
        return new Reader<TEnv, T>(f);
    }
}


