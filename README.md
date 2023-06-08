# ffc-payment-statement-integrations
Logic Apps to integrate the payment statement generator with Dynamics 365

## Logic Apps (Standard)
This codebase uses Logic Apps (Standard) as opposed to Logic Apps (Consumption) due to the ability to unit test and better control the apps. Unlike Consumption Logic Apps which can only have one workflow, Standard Logic Apps can have multiple workflows that sit below the App Service.

## Workflows
This codebase contains two workflows:
CrmInsert - receives Service Bus topic messages and updates entries in Dynamics 365
CrmRetrieval - recieves an HTTP request and serves the requested PDF content

## Design or edit the workflows
To use the GUI editor locally, install a plugin/extension for either VS Code or Visual Studio. Note that the Visual Studio plugin only runs on VS2019 under Windows.
See instructions on how to set up VS Code https://learn.microsoft.com/en-us/azure/logic-apps/create-single-tenant-workflows-visual-studio-code

## Run the unit tests
Azurite must be running in order for the unit tests to run properly. Azurite can be installed as an extension to VS Code or stand-alone.
If you do not have a local.settings.json file, create one by copying local.settings.unit-test.json

## Known issues
- There are a few differences in the validation logic between the current VS Code editor and the Azure Portal online editor. For example, when using the 'splitOn' functionality to (for example) create an array from a series of Service Bus messages, the Portal editor does not allow a Response action whereas the VS Code editor does.
- There is a bug in the VS Code editor where copy/paste does not work inside an action/trigger edit panel when using the shortcut keys, but does work if you use the main edit menu selections.
- local.settings.json is used to store connection strings and other connection information when using the editor. Sometimes secrets may get saved in this file so it is excluded from commits to GitHub. However, the unit tests require this file to exist (see 'Run the unit tests')