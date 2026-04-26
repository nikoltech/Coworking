Issues

-- SquidexApiClient.GetAppLocalesAsync 
returns a list of strings representing the locales available in the Squidex application.
But responce have and other fields like IsMaster and IsOptional and are not being returned by the method. 
In the meantime, you can set DefaultLocale in appsettings.json to correctly configure the default locale.

-- SquidexLocaleProvider.InitializeAsync 
initializes locales from appsettings.json or from the Squidex API.
But it does not use the IsMaster field to set the DefaultLocale in options. It takes from appsettings.json or by constant SquidexLocales.En value.