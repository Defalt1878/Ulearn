# Консольное приложение Антиплагиата

[Ссылка на инструкцию для пользователя](https://docs.google.com/document/d/1WcYcVSd0GfRFyfXd6JeUFhMr_cdX120HJdrGtGuCO58/edit?usp=sharing)

Для запуска приложения требуется запустить AntiPlagiarism.ConsoleApp.exe

__Порядок публикации билда:__

1. Right-click по проекту и выбор опции Publish
2. Выбрать опцию Local Folder
3. В Target Location указать пустую папку для публикации
4. Configuration: Release | Any CPU, Target Framework: net6.0
5. Deployment mode: Framework-Dependent. Вариант деплоя Self-Contained недоступен, поскольку приложение ссылается на
   проект uLearn.Common
6. Target Runtime: Portable (для запуска как на win-x64, так и win-x86)
