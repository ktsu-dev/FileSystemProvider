// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider;

using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering FileSystemProvider with dependency injection
/// </summary>
public static class FileSystemProviderExtensions
{
	/// <summary>
	/// Adds FileSystemProvider services to the service collection as a singleton
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <returns>The service collection for method chaining</returns>
	public static IServiceCollection AddFileSystemProvider(this IServiceCollection services) =>
		services.AddSingleton<IFileSystemProvider, FileSystemProvider>();

	/// <summary>
	/// Adds FileSystemProvider services to the service collection with configuration options
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="options">Configuration options for the FileSystemProvider</param>
	/// <returns>The service collection for method chaining</returns>
	public static IServiceCollection AddFileSystemProvider(this IServiceCollection services, FileSystemProviderOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		return services.AddSingleton<IFileSystemProvider>(_ => new FileSystemProvider(options));
	}

	/// <summary>
	/// Adds FileSystemProvider services to the service collection with configuration options
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="configureOptions">Action to configure the FileSystemProvider options</param>
	/// <returns>The service collection for method chaining</returns>
	public static IServiceCollection AddFileSystemProvider(this IServiceCollection services, Action<FileSystemProviderOptions> configureOptions)
	{
		ArgumentNullException.ThrowIfNull(configureOptions);

		FileSystemProviderOptions options = new();
		configureOptions(options);

		return services.AddSingleton<IFileSystemProvider>(_ => new FileSystemProvider(options));
	}

	/// <summary>
	/// Adds FileSystemProvider services to the service collection with a custom factory
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="factory">Factory function to create the FileSystemProvider instance</param>
	/// <returns>The service collection for method chaining</returns>
	public static IServiceCollection AddFileSystemProvider(this IServiceCollection services, Func<IServiceProvider, IFileSystemProvider> factory) =>
		services.AddSingleton(factory);
}
