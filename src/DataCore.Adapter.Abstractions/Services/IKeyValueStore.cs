﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// A service that can be used to store arbitrary key-value pairs.
    /// </summary>
    /// <remarks>
    ///   <see cref="IKeyValueStore"/> is intended to allow an adapter to store arbitrary 
    ///   key-value data that can be persisted between restarts of the adapter or its host 
    ///   application.
    /// </remarks>
    /// <seealso cref="InMemoryKeyValueStore"/>
    /// <seealso cref="ScopedKeyValueStore"/>
    /// <seealso cref="KeyValueStoreExtensions"/>
    public interface IKeyValueStore {

        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(byte[] key, TValue? value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(byte[] key);


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        ValueTask<KeyValueStoreOperationStatus> DeleteAsync(byte[] key);


        /// <summary>
        /// Gets the keys that are defined in the store.
        /// </summary>
        /// <returns>
        ///   The keys.
        /// </returns>
        IEnumerable<byte[]> GetKeys();

    }
}
