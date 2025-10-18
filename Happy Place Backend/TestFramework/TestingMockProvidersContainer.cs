using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace
{
    public class TestingMockProvidersContainer : IDisposable
    {
        // Fields
        private InMemoryEmailSenderProvider _emailProvider;
        private bool _isDisposed;
        private WebClient _webClient;

        // Constructors
        public TestingMockProvidersContainer()
        {
            this._webClient = new WebClient();
            this._emailProvider = new InMemoryEmailSenderProvider();
        }
        ~TestingMockProvidersContainer()
        {
            Dispose(disposing: false);
        }

        // Properties
        public InMemoryEmailSenderProvider EmailProvider => this._emailProvider;
        public WebClient WebClient => this._webClient;

        // Methods
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                this.DisposeProviders();
                _isDisposed = true;
            }
        }

        private void DisposeProviders()
        {
            this._emailProvider?.Dispose();
            this._emailProvider = null;
            this._webClient?.Dispose();
            this._webClient = null;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
