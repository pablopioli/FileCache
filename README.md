## What you need to know

This library is a file-based cache layer for CacheTower
(https://github.com/TurnerSoftware/CacheTower).

It stores cache items as files in a folder in Json format. It’s designed to be an auxiliary layer to an in-memory caching.

There is already an official layer that does this (https://www.nuget.org/packages/CacheTower.Providers.FileSystem.Json/). 
But the differences this library brings in are:

* Is resistant to application or system crashes. You don’t lose your cache items in these cases. (https://github.com/TurnerSoftware/CacheTower/issues/199)
* Uses System.Text.Json instead of Newtonsoft.Json

Available on Nuget as **ppioli.FileCache**
