# SCH-Pincér

`view-source:https://schpincer.sch.bme.hu/`
```html
<table class="circles-table">
    <tr class="orange">
        <td class="border-colored">
            <a href="/provider/americano/">Americano</a>
        </td>
        <td>Kedd</td>
        <td class="date">20:00 (24-11-26)</td>
        <td class="feeling">Back to the Americano</td>
        <td class="arrow">
            <a href="/provider/3/"><i class="material-icons">navigate_next</i></a>
        </td>
    </tr>
    <tr class="red">
        <td class="border-colored">
            <a href="/provider/kakas/">Kakas</a>
        </td>
        <td>Szerda</td>
        <td class="date">19:30 (24-11-27)</td>
        <td class="feeling">Kakas Nyitás</td>
        <td class="arrow">
            <a href="/provider/5/"><i class="material-icons">navigate_next</i></a>
        </td>
    </tr>
    <tr class="yellow">
        <td class="border-colored">
            <a href="/provider/reggelisch/">ReggeliSCH</a>
        </td>
        <td>Csütörtök</td>
        <td class="date">07:36 (24-11-28)</td>
        <td class="feeling">Reggelisch nyitás</td>
        <td class="arrow">
            <a href="/provider/49/"><i class="material-icons">navigate_next</i></a>
        </td>
    </tr>
    <tr class="green">
        <td class="border-colored">
            <a href="/provider/magyarosch/">Magyarosch</a>
        </td>
        <td>Csütörtök</td>
        <td class="date">19:00 (24-11-28)</td>
        <td class="feeling">marhapöri</td>
        <td class="arrow">
            <a href="/provider/29298/"><i class="material-icons">navigate_next</i></a>
        </td>
    </tr>
</table>
```

Thymeleaf:
```html
<table class="circles-table">
    <tr th:each="opening : ${openings}" th:object="${opening}" th:class="*{circle.cssClassName}" class="purple">
        <td class="border-colored">
            <a href="#" th:text="*{circle.displayName}" th:href="@{/provider/__*{circle.alias}__/}">Vödör</a>
        </td>
        <td th:text="#{lang.weekday-__${timeService.format(opening.dateStart, 'u')}__}">Hetfő</td>
        <td class="date" th:text="${timeService.format(opening.dateStart, '__#{lang.date-opening-format}__')}">18:00
            (18-04-09)
        </td>
        <td class="feeling" th:text="*{feeling}">Lorem ispsum dolor sit amet.</td>
        <td class="arrow"><a href="#" th:href="@{/provider/__*{circle.id}__/}"><i class="material-icons">navigate_next</i></a>
        </td>
    </tr>
</table>
```

Blazor:
```html
<table class="circles-table">
    @foreach (var opening in Openings)
    {
        <tr class="@opening.Circle.CssClassName">
            <td class="border-colored">
                <a href="@($"/provider/{opening.Circle.Alias}")">@opening.Circle.DisplayName</a>
            </td>
            <td>@opening.DateStart.ToString("dddd")</td>
            <td class="date">@opening.DateStart.ToString("yyyy-MM-dd")</td>
            <td class="feeling">@opening.Feeling</td>
            <td class="arrow">
                <a href="@($"/provider/{opening.Circle.Id}")"><i class="material-icons">navigate_next</i></a>
            </td>
        </tr>
    }
</table>
```