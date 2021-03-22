using Blazored.SessionStorage.StorageOptions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Blazored.SessionStorage
{
    public class SessionStorageService : ISessionStorageService, ISyncSessionStorageService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IJSInProcessRuntime _jSInProcessRuntime;
        private readonly JsonSerializerSettings _jsonOptions;

        public event EventHandler<ChangingEventArgs> Changing;

        public event EventHandler<ChangedEventArgs> Changed;

        public SessionStorageService(IJSRuntime jSRuntime, IOptions<SessionStorageOptions> options)
        {
            _jSRuntime = jSRuntime;
            _jsonOptions = options.Value.JsonSerializerSettings;
            _jSInProcessRuntime = jSRuntime as IJSInProcessRuntime;
        }

        public async Task SetItemAsync<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var e = await RaiseOnChangingAsync(key, data);

            if (e.Cancel)
                return;

            var serializedData = JsonConvert.SerializeObject(data, _jsonOptions);
            //var serialisedData = JsonSerializer.Serialize(data, _jsonOptions);

            await _jSRuntime.InvokeVoidAsync("sessionStorage.setItem", key, serializedData);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var serializedData = await _jSRuntime.InvokeAsync<string>("sessionStorage.getItem", key);

            if (serializedData == null)
                return default;

            return JsonConvert.DeserializeObject<T>(serializedData, _jsonOptions);
            //return JsonSerializer.Deserialize<T>(serialisedData, _jsonOptions);
        }

        public async Task RemoveItemAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            await _jSRuntime.InvokeAsync<object>("sessionStorage.removeItem", key);
        }

        public async Task ClearAsync() => await _jSRuntime.InvokeAsync<object>("sessionStorage.clear");

        public async Task<int> LengthAsync() => await _jSRuntime.InvokeAsync<int>("eval", "sessionStorage.length");

        public async Task<string> KeyAsync(int index) => await _jSRuntime.InvokeAsync<string>("sessionStorage.key", index);

        public async Task<bool> ContainKeyAsync(string key) => await _jSRuntime.InvokeAsync<bool>("sessionStorage.hasOwnProperty", key);

        public void SetItem<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            var e = RaiseOnChangingSync(key, data);

            if (e.Cancel)
                return;

            var serializedData = JsonConvert.SerializeObject(data, _jsonOptions);
            //var serialisedData = JsonSerializer.Serialize(data, _jsonOptions);

            _jSInProcessRuntime.InvokeVoid("sessionStorage.setItem", key, serializedData);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public T GetItem<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            var serializedData = _jSInProcessRuntime.Invoke<string>("sessionStorage.getItem", key);

            if (serializedData == null)
                return default;

            return JsonConvert.DeserializeObject<T>(serializedData, _jsonOptions);
            //return JsonSerializer.Deserialize<T>(serialisedData, _jsonOptions);
        }

        public void RemoveItem(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            _jSInProcessRuntime.InvokeVoid("sessionStorage.removeItem", key);
        }

        public void Clear()
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            _jSInProcessRuntime.InvokeVoid("sessionStorage.clear");
        }

        public int Length()
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            return _jSInProcessRuntime.Invoke<int>("eval", "sessionStorage.length");
        }

        public string Key(int index)
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            return _jSInProcessRuntime.Invoke<string>("sessionStorage.key", index);
        }

        public bool ContainKey(string key)
        {
            if (_jSInProcessRuntime == null)
                throw new InvalidOperationException("IJSInProcessRuntime not available");

            return _jSInProcessRuntime.Invoke<bool>("sessionStorage.hasOwnProperty", key);
        }

        private async Task<ChangingEventArgs> RaiseOnChangingAsync(string key, object data)
        {
            var e = new ChangingEventArgs
            {
                Key = key,
                OldValue = await GetItemAsync<object>(key),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        private ChangingEventArgs RaiseOnChangingSync(string key, object data)
        {
            var e = new ChangingEventArgs
            {
                Key = key,
                OldValue = ((ISyncSessionStorageService)this).GetItem<object>(key),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        private void RaiseOnChanged(string key, object oldValue, object data)
        {
            var e = new ChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = data
            };

            Changed?.Invoke(this, e);
        }
    }
}