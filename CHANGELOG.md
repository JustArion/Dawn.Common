# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Development]

## [1.0.1] / 2026-09-18
- Added overload `LoggerConfiguration.AddCommon(CommonLoggingOptions, DirectoryInfo)`
- `LoggerConfiguration.AddCommon` now uses the CurrentDirectory over the BaseDirectory.
- `Dawn.Common.Windows`' "Static" class is now an extension on-top of the `Dawn.Common`'s "Static" class

## [1.0.0] / 2026-09-18
- Initial Release

[Development]: https://github.com/JustArion/Dawn.Common/compare/1.0.1...HEAD
[1.0.1]: https://github.com/JustArion/Dawn.Common/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/JustArion/Dawn.Common/tree/1.0.0
