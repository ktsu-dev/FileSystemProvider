// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider;

using System;
using System.IO.Abstractions;
using System.Threading;

/// <summary>
/// Provides access to filesystem implementations with support for dependency injection
/// </summary>
public class FileSystemProvider : IFileSystemProvider
{
	// Use the Lazy<T> pattern for thread-safe initialization of the default instance
	private readonly Lazy<IFileSystem> _defaultInstance = new(() => new FileSystem());

	// Lock to protect shared state modifications
	private readonly Lock _lock = new();

	// Shared factory for testing - set once before tests, cleared once after tests
	private Func<IFileSystem>? _testFactory;

	// Use AsyncLocal to cache the created instance per async execution context
	// This gets replaced entirely when we need to clear all contexts
	private AsyncLocal<IFileSystem?> _asyncLocalCache = new();

	/// <summary>
	/// Gets the current filesystem instance
	/// </summary>
	public IFileSystem Current
	{
		get
		{
			// First check if we have a cached instance for this async context
			IFileSystem? cachedInstance = _asyncLocalCache.Value;
			if (cachedInstance != null)
			{
				return cachedInstance;
			}

			// Check if we have a test factory set (with lock protection)
			Func<IFileSystem>? factory;
			lock (_lock)
			{
				factory = _testFactory;
			}

			if (factory != null)
			{
				// Create and cache the instance for this async context
				IFileSystem newInstance = factory();
				_asyncLocalCache.Value = newInstance;
				return newInstance;
			}

			// Otherwise return the default lazy-initialized instance
			return _defaultInstance.Value;
		}
	}

	/// <summary>
	/// Sets a shared filesystem factory for testing.
	/// Each async context will call this factory to get its own isolated instance.
	/// This should be set once before tests and cleared once after tests.
	/// </summary>
	/// <param name="factory">A factory function that creates filesystem instances</param>
	public void SetFileSystemFactory(Func<IFileSystem> factory)
	{
		ArgumentNullException.ThrowIfNull(factory);

		lock (_lock)
		{
			_testFactory = factory;
			// Replace the entire AsyncLocal to clear all cached instances
			_asyncLocalCache = new();
		}
	}

	/// <summary>
	/// Resets the filesystem back to the default implementation for ALL contexts
	/// </summary>
	public void ResetToDefault()
	{
		lock (_lock)
		{
			_testFactory = null;
			// Replace the entire AsyncLocal to clear all cached instances across all async contexts
			_asyncLocalCache = new();
		}
	}
}
