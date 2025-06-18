// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider;

using System;
using System.IO.Abstractions;

/// <summary>
/// Interface for providing access to filesystem implementations
/// </summary>
public interface IFileSystemProvider
{
	/// <summary>
	/// Gets the current filesystem instance
	/// </summary>
	public IFileSystem Current { get; }

	/// <summary>
	/// Gets whether the provider is currently in test mode
	/// </summary>
	public bool IsInTestMode { get; }

	/// <summary>
	/// Sets a filesystem factory for testing
	/// </summary>
	/// <param name="factory">A factory function that creates filesystem instances</param>
	/// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
	/// <exception cref="FileSystemProviderException">Thrown when factory returns null or configuration is invalid</exception>
	public void SetFileSystemFactory(Func<IFileSystem> factory);

	/// <summary>
	/// Resets the filesystem back to the default implementation
	/// </summary>
	public void ResetToDefault();
}
