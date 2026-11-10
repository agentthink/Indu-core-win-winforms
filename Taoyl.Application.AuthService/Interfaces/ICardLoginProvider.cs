using System;

namespace Taoyl.Application.AuthService.Interfaces
{
    public interface ICardLoginProvider
    {
        event Action<string> CardRead;
        bool IsEnabled { get; } 
        void Start();
        void Stop();
    }

}
