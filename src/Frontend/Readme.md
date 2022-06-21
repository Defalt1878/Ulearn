Это краткий документ о том, как устроен фронт.

### Сборки и конфиги

Имеется 3 типа сборки: development, production. У каждого типа свой конфиг webpack-а.

Запуск Node-ом описан в файлах в папке scripts. В dev режиме запускается dev server для работы в браузере, подключается HMR.

    "start": "node scripts/start.js", -- dev
    "build": "node scripts/build.js", -- prod
    
Конфиги webpack описаны в папке config (webpack.config.(dev||prod)). 
Имеется базовый конфиг webpack.config.base, в котором описаны alias-ы (они нужны, чтобы вместо @skbkontur/react-ui писать ui).
Так же в этой папке имеются конфиги полифилов, сентри (логгирование ошибок) и т.д.
    
Имеются дополнительные возможности.
    
    "analyze": "node scripts/build.js analyze", -- запуст production с bundleAnalyzer, для анализа сборки
    "test": "jest", -- запуск тестов
    "storybook": "start-storybook -p 9001 -c .storybook" -- запуск сторибука
    
Настройки бабеля находятся в ./babel.config.js


### Взаимодействие сервер-клиент

Имеется 2 проекта на бэке, от которых зависит фронт: Api и Web. 

Api отвечает за работу базы данных, запросы на ручки и тд (имеет адрес http:[::]:8000, api.ulearn.me).

Web запускает скрипт build.js, берет полученную (статичную) сборку, и отдает её на запросы в браузере.
Также отвечает за рендер cshtml страниц (https:[::]:44300, http://localhost:53834/, ulearn.me).

У фронта же есть dev server (http:[::]:3000).

Всю нужную информацию, авторизацию получаем от api.

### Как работает DevServer

В оперативную память кладется сборка, которая получается в скрипте webpack.config.dev.
хоститься веб на [::]:3000, при любом запросе на этот адресс (localhost:3000, localhost:3000/123/123 и т.д.)
отдается index.html с бандлами скриптов (в общем заменяет web).
Далее, если нужна легаси страница, запрос прокситься на web(44300).
А api запрос отправляется по 8000 адресу, который мы получаем из window.config.

На боевом это пропишет сервер в теге script (когда будет возвращать index.html).

```
window.config={
  "api": {
    "endpoint": "https://api.ulearn.me/"
  }
}
```

на локале это прописано в src/config.ts (он берет адресс из settings.json).

### Структура фронта

Входная точка - app.tsx (index.tsx рендерит эту компоненту и подключает serviceWorker)
в app.tsx мы также добавляем отловку ошибок в ErrorBoundary (сюда же можно отнести и 404 страницу :)).

Компоненты находятся в папках src/pages и src/components.
в pages обычно помещаются не сами компоненты, а их версия с подключением к redux (connect) или react-router-dom.

#### Redux и api

В App.js мы создаем redux хранилище, он состоит из мн-ва reducer-ов, их список можно посмотреть в src/redux/reducers. 
начальное состояние хранилища (дефолтные данные) описаны в разных файлах в папке src/redux
(например данные об аккаунте в redux/account, данные о курсах в redux/course).

Любые api запросы могут добавлять/изменять/удалять данные хранилища.

Все api запросы описаны в api (они также разбиты на запросы для курсов,аккаунта и тд).

Actions описаны в src/actions.

#### Навигация

Для навигации мы используем react-router-dom, он сверяет адресную строку с паттернами/строками, описанными в router.js
и рендерит подходящую компоненту, например для пути course/{courseId} отрендериться компонента course.

```
<Route path="/Admin/Groups" component={redirectLegacyPage("/:courseId/groups")}/>

<Route path="/course/:courseId/:slideSlugOrAction" component={Course}/>

<Route path="/:courseId/groups/" component={GroupListPage} exact/>
<Route path="/:courseId/groups/:groupId/" component={GroupPage} exact/>
<Route path="/:courseId/groups/:groupId/:groupPage" component={GroupPage} exact/>

<Route component={AnyPage}/>
```

### Тесты
тесты запускаются локально с помощью storybook + loki.
Для начала надо запустить сторибук, после чего запустить тесты loki.
```
npm run storybook
yarn loki test
```
loki/current эталонные скриншоты.

loki/difference не пройденные тесты.

yarn loki approve принять все изменения.

yarn loki test laptop тесты только на определенную платформу.

yarn loki update добавить скриншоты в эталон.

### Разные вспомогательные штуки  

Все константы находятся в src/consts.

Имеются вспомогательные функции (перевод в camelCase, склонение слова в зависимости от числа и тд) в src/utils.

в src/codeTranslator имеются функции, которые переводят текст katex и codeMirror.

## Доп информация

Мы подключаем полифилы с помощью babel useBuiltsIn

Подключаем oldBrowser, которые просит обновить браузер, если он устарел, список поддерживаемых браузеров находиться в .browserlistrc.

Папка build содержит последнюю сборку, полученную из webpack.config.prod (которую забирает web).
