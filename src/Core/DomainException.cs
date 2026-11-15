namespace Core;

// Порушення бізнес-правила (стан об'єкта не дозволяє операцію).
// Аргументні перевірки лишаються ArgumentException; технічні помилки (IO, JSON) не маскуються.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
