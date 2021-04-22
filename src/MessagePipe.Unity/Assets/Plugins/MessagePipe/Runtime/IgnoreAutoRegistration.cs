using System;

namespace MessagePipe
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class IgnoreAutoRegistration : Attribute
    {
    }
}
