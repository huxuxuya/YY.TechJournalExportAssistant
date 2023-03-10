# Документация и примеры использования

Подраздел содержит информацию о примерах использования технологического журнала в базе ClickHouse, в т.ч. примеры настройки, тексты зарпосов для анализа данных и многое другое.

## Примеры запросов

Стоит заметить, что это лишь примеры запросов и часто для анализа их нужно подгонять под свою ситуацию, тем более вариантов настроек сбора самого технологического журнала огромное количество.

Также ситуация усложняется тем, что в зависимости от версии платформы 1С технологический журнал имеет свои особенности работы, которые иногда не поддаются логике или "регилиозным догматам".

Примеры запросов для анализа технологического журнала:

* Ошибки и информация об их появлении.
* Долгие запросы с контекстом выполнения.
* Сбор данных полностью выключен.
* Сбор всех событий.
* Информация об управляемых блокировках.
* Информация о блокировках СУБД.
* Сбор всех событий (клиентский компьютер).
* Взаимодействие с пользовательским интерфейсом.

## Примеры настроек технологического журнала

Примеры файлов настроек технологического журнала для сбора различных событий (не забываем файл конфигурации приводить к нормальному виду по имени и содержимому перед сбором).

Настройки были взяти из разных источников или сформированы с нуля. Например, информацию по событиям, работе с ТЖ и примеры настроек можно узнать здесь:
* [ИТС](https://its.1c.ru/db/v8319doc#bookmark:adm:ti000000157)
* [Информация о событиях](https://infostart.ru/1c/articles/1195695/)
* [Как настраивать](https://its.1c.ru/db/freshpub/content/34/hdoc)
* [Инструменты разработчика](http://devtool1c.ucoz.ru/index/opisanie_podsistemy/0-4) от [Сергея Старых](https://github.com/tormozit). В том числе на [GitHub](https://github.com/tormozit).

Все примеры:
* Исключительные ситуации и административные действия (для 8.2 и более расширенный длоя 8.3).
* Долгие запросы (дольше 5 секунд, для MS SQL Server).
* Взаимодействие с пользовательским интерфейсом.
* Диагностика утечек памяти.
* Информация о блокировках СУБД.
* Информация о реструктуризации базы данных.
* Информация об управляемых блокировках.
* Сбор всех событий (клиентский компьютер).
* Сбор всех событий обращения к СУБД и планы запросов.
* Сбор всех событий обращения к СУБД.
* Сбор всех событий.
* Сбор данных полностью выключен.

## Далее

Другая информация в разработке и будет выложена по мере готовности.