# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

## [1.1.0] - 2023-01-06
### Added
- Unit test for `CoroutineTask.Delay()` method.
- Methods and property for creating already finished tasks:
  - `CoroutineTask.CompletedTask`.
  - `CoroutineTask.FromCancelled` and `CoroutineTask.FromCancelled<T>`
  - `CoroutineTask.FromException` and `CoroutineTask.FromException<T>`
  - `CoroutineTask.FromResult<T>`

## [1.0.0] - 2023-01-05
This is the first release of *CoroutineEx*.