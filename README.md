# ktsu.FileSystemProvider

[![NuGet](https://img.shields.io/nuget/v/ktsu.FileSystemProvider.svg)](https://www.nuget.org/packages/ktsu.FileSystemProvider/)
[![Build Status](https://github.com/ktsu-dev/FileSystemProvider/workflows/CI/badge.svg)](https://github.com/ktsu-dev/FileSystemProvider/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A simple, thread-safe provider for centralized filesystem access in .NET applications using `System.IO.Abstractions`.

## ✨ Features

- **🔧 Thread-Safe**: Uses `AsyncLocal<T>` for safe concurrent access across async contexts
- **🧪 Testable**: Factory pattern for creating isolated mock filesystems in tests
- **⚡ Lazy Initialization**: Default filesystem instance is created only when needed
- **🎯 Simple API**: Static class with minimal surface area focused on two use cases
- **🔄 Context Isolation**: Each async context gets its own filesystem factory
- **🏭 Testing-Focused**: Factory pattern specifically designed for test isolation
- **📦 Zero Configuration**: Works out of the box with sensible defaults

## 🚀 Quick Start

### Installation

```bash
dotnet add package ktsu.FileSystemProvider
```

### Production Usage

```csharp
using ktsu.FileSystemProvider;

// Use the default filesystem (shared, lazy-initialized)
var fileSystem = FileSystemProvider.Current;
fileSystem.File.WriteAllText("test.txt", "Hello World!");

// Read from file
string content = fileSystem.File.ReadAllText("test.txt");
```

### Testing Usage

```csharp
using System.IO.Abstractions.TestingHelpers;
using ktsu.FileSystemProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MyTestClass
{
    [ClassInitialize]
    public static void TestFixtureSetup(TestContext context)
    {
        // Set factory once for all tests
        FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [ClassCleanup]
    public static void TestFixtureTeardown()
    {
        // Clear factory once after all tests
        FileSystemProvider.ResetToDefault();
    }

    [TestMethod]
    public void MyTest()
    {
        // Each test gets its own isolated MockFileSystem instance
        var fileSystem = FileSystemProvider.Current;
        fileSystem.File.WriteAllText("test.txt", "Hello World!");
        
        // Verify using the current instance for this test
        var mockFileSystem = (MockFileSystem)FileSystemProvider.Current;
        Assert.IsTrue(mockFileSystem.File.Exists("test.txt"));
    }
}
```

## 📖 API Reference

### FileSystemProvider

Static class providing centralized filesystem access with two modes:
- **Production**: Shared default filesystem instance
- **Testing**: Factory-created isolated instances

#### Properties

- **`Current`** - Gets the current filesystem instance (IFileSystem)

#### Methods

- **`SetFileSystemFactory(Func<IFileSystem> factory)`** - Sets a factory for creating test filesystem instances
- **`ResetToDefault()`** - Resets to the default production filesystem

## 🔄 Usage Patterns

### Production Pattern (Default)
```csharp
// No setup needed - just use the current filesystem
var fileSystem = FileSystemProvider.Current; // Always returns the same shared FileSystem instance
fileSystem.File.WriteAllText("data.txt", "production data");
```

### Testing Pattern (Factory)
```csharp
// Set up factory once per test
FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

// First access creates and caches a MockFileSystem instance per async context
var fs1 = FileSystemProvider.Current; // New MockFileSystem instance
var fs2 = FileSystemProvider.Current; // Same cached instance (preserves test state)

// Different async contexts get their own isolated instances
Task.Run(() => FileSystemProvider.Current); // Different MockFileSystem instance
```

## 🧪 Testing Examples

### Basic Unit Test
```csharp
[TestClass]
public class DocumentServiceTests
{
    [ClassInitialize]
    public static void TestFixtureSetup(TestContext context)
    {
        FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [ClassCleanup]
    public static void TestFixtureTeardown()
    {
        FileSystemProvider.ResetToDefault();
    }

    [TestMethod]
    public void FileSystem_CreatesFile_Successfully()
    {
        // Arrange - Each test gets its own isolated MockFileSystem
        var fileSystem = FileSystemProvider.Current;
        
        // Act
        fileSystem.File.WriteAllText("test.txt", "content");
        
        // Assert
        var mockFileSystem = (MockFileSystem)FileSystemProvider.Current;
        Assert.IsTrue(mockFileSystem.File.Exists("test.txt"));
        Assert.AreEqual("content", mockFileSystem.File.ReadAllText("test.txt"));
    }
}
```

### Parallel Test Isolation
```csharp
[TestClass]
public class ParallelTests
{
    [ClassInitialize]
    public static void TestFixtureSetup(TestContext context)
    {
        FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
    }

    [ClassCleanup]
    public static void TestFixtureTeardown()
    {
        FileSystemProvider.ResetToDefault();
    }

    [TestMethod]
    public void ParallelTests_AreIsolated()
    {
        // Act - Run parallel tests
        Parallel.For(0, 10, i =>
        {
            var fileSystem = FileSystemProvider.Current;
            fileSystem.File.WriteAllText($"test{i}.txt", $"content{i}");
            
            // Each parallel execution gets its own MockFileSystem
            var mockFS = (MockFileSystem)FileSystemProvider.Current;
            Assert.IsTrue(mockFS.File.Exists($"test{i}.txt"));
        });
    }
}
```

### Integration with Dependency Injection

```csharp
public class DocumentService
{
    private readonly IFileSystem _fileSystem;
    
    public DocumentService(IFileSystem fileSystem = null)
    {
        _fileSystem = fileSystem ?? FileSystemProvider.Current;
    }
    
    public void SaveDocument(string path, string content)
    {
        _fileSystem.File.WriteAllText(path, content);
    }
    
    public string LoadDocument(string path)
    {
        return _fileSystem.File.ReadAllText(path);
    }
}
```

## 🏗️ Implementation Details

### Lazy Initialization
The default filesystem instance is created using `Lazy<T>` to ensure thread-safe, one-time initialization:

```csharp
private static readonly Lazy<IFileSystem> _defaultInstance = new(() => new FileSystem());
```

### Async Context Isolation
A shared factory for testing with per-context instance caching:

```csharp
private static Func<IFileSystem>? _testFactory; // Shared across all contexts
private static AsyncLocal<IFileSystem?> _asyncLocalCache = new(); // Cached per context
```

### Two-Mode Design
- **Production Mode**: `Current` returns the shared default `FileSystem` instance
- **Testing Mode**: `Current` calls the shared factory (once per async context) and caches the result

## 📋 Best Practices

1. **Production**: Just use `FileSystemProvider.Current` - no setup needed
2. **Testing**: Set factory once in test fixture setup, clear once in teardown
3. **Test isolation**: Each test/async context gets its own MockFileSystem instance automatically
4. **Parallel tests**: Factory pattern automatically provides isolation
5. **Service constructors**: Use `FileSystemProvider.Current` as default parameter for dependency injection

### Recommended Test Setup Pattern
```csharp
[TestClass]
public class MyTests
{
    [ClassInitialize]
    public static void Setup(TestContext context) => FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

    [ClassCleanup] 
    public static void Teardown() => FileSystemProvider.ResetToDefault();

    [TestMethod]
    public void MyTest()
    {
        // Each test gets its own isolated MockFileSystem - no additional setup needed
        var fs = FileSystemProvider.Current;
        // ... test code
    }
}
```

## 🔧 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
