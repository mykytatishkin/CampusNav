# CampusNav — Инструкция по проекту

## Обзор проекта

CampusNav — приложение для 3D-навигации по кампусу VILNIUS TECH (VGTU) Saulėtekio Rūmai.
Поддерживает GPS-геолокацию, 3 алгоритма поиска пути, орбитальную камеру, управление лифтами.

**Unity версия:** 2023 LTS (URP 17.2)
**Платформы:** Android, iOS, Editor

---

## Структура проекта

```
Assets/Scripts/
├── CampusNavigator.cs            — Главный контроллер навигации (NavMesh/A*/Dijkstra)
├── CampusCameraController.cs     — Орбитальная камера вокруг маркера
├── PlayerGeoTracker.cs           — GPS-трекинг игрока + авто-фокус на кампусе
├── GeoAnchorSystem.cs            — Сервис геолокации + запрос разрешений + границы
├── BuildingRevealer.cs           — Прозрачность зданий при входе игрока
├── NavigationPreferences.cs      — Настройки: лифты, доступные маршруты
├── NavigationSettingsUI.cs       — UI настроек (toggles, камера, этаж)
├── RouteSelectionUI.cs           — UI выбора маршрута (поиск, фильтры)
├── ElevatorLink.cs               — NavMeshLink-обёртка для лифтов
│
├── Data/
│   ├── RoutePoint.cs             — ScriptableObject: точка маршрута
│   ├── RouteDatabase.cs          — Коллекция RoutePoint + поиск/фильтрация
│   ├── RoutePointCategory.cs     — Enum: 16 категорий (Classroom, WC, SmokingArea...)
│   └── GeoReference.cs           — GPS <-> Unity координаты (VGTU Saulėtekio)
│
├── Pathfinding/
│   ├── IPathfinder.cs            — Интерфейс + PathResult struct
│   ├── NavigationGraph.cs        — Граф: ноды, рёбра, построение из RoutePoints
│   ├── AStarPathfinder.cs        — A* (эвклидова эвристика)
│   ├── DijkstraPathfinder.cs     — Dijkstra (полный перебор)
│   ├── NavMeshPathfinder.cs      — Обёртка Unity NavMesh
│   ├── PathfindingBenchmark.cs   — Бенчмарк: сравнение 3 алгоритмов + Gizmos
│   └── BenchmarkUI.cs            — Runtime UI для симуляционной сцены
│
└── Editor/
    ├── CampusSceneGenerator.cs   — Генератор основной сцены кампуса
    ├── RoutePointGenerator.cs    — Автогенерация ~180+ RoutePoint ассетов
    └── BenchmarkSceneGenerator.cs— Генератор тестовой сцены для сравнения алгоритмов
```

---

## Здания кампуса

| Здание   | Полное название                    | Этажей | Размер (Unity) |
|----------|------------------------------------|--------|----------------|
| SRK-I    | Saulėtekio rūmai, korpusas I       | 4      | 55 × 18        |
| SRK-II   | Saulėtekio rūmai, korpusas II      | 3      | 18 × 45        |
| SRA-I    | Saulėtekio rūmai, administracinis I| 4      | 38 × 22        |
| SRA-II   | Saulėtekio rūmai, administracinis II| 3     | 32 × 18        |
| SRC      | Saulėtekio rūmai, centrinis        | 3      | 28 × 28        |
| SRL-I    | Saulėtekio rūmai, laboratorinis I  | 6      | 28 × 18        |
| SRL-II   | Saulėtekio rūmai, laboratorinis II | 3      | 50 × 18        |

---

## GPS-координаты

| Параметр | Значение |
|----------|----------|
| Якорная точка (вход SRK-I) | 54.6898°N, 25.2888°E |
| Центр кампуса | ~54.6893°N, ~25.2912°E |
| Граница: юг | 54.6875°N |
| Граница: север | 54.6910°N |
| Граница: запад | 25.2860°E |
| Граница: восток | 25.2960°E |
| Территория | ~390m × 570m |
| Поворот севера (northRotationOffset) | 25° (требует калибровки) |

---

## Таблица сравнения алгоритмов поиска пути

| Свойство | NavMesh (Unity) | A* | Dijkstra |
|---|---|---|---|
| **Тип** | Mesh-based (нативный C++) | Граф + эвристика | Граф, полный перебор |
| **Оптимальность** | Кратчайший путь на mesh | Кратчайший на графе | Кратчайший на графе |
| **Скорость** | Самый быстрый | Быстрый (меньше нод) | Самый медленный |
| **Узлы исследовано** | N/A (чёрный ящик) | Меньше чем Dijkstra | Все до цели |
| **3D / многоэтажность** | Через NavMeshLink | Через граф + лифт-рёбра | Через граф + лифт-рёбра |
| **Лифты вкл/выкл** | NavMeshLink.enabled | Фильтр рёбер | Фильтр рёбер |
| **Гибкость** | Только walkable mesh | Произвольный граф | Произвольный граф |
| **Память** | Baked mesh | O(V + E) | O(V + E) |
| **Сложность** | Скрытая (оптимизированная) | O(E log V) | O((V + E) log V) |
| **Когда использовать** | Основной для навигации | Большие графы, нужна скорость | Малые графы, гарантия полноты |

### Как работает каждый алгоритм

**NavMesh** — Unity запекает полигональную сетку walkable-поверхностей. Поиск пути идёт по готовому мешу через оптимизированный C++ код. Самый быстрый, но нужно перезапекать при изменении геометрии.

**A\*** — строит граф из RoutePoint'ов. Использует эвклидово расстояние до цели как эвристику, чтобы приоритизировать направление к цели. Исследует меньше узлов, чем Dijkstra.

**Dijkstra** — как A*, но без эвристики. Равномерно расширяется во все стороны от старта. Гарантирует кратчайший путь, но исследует больше узлов.

---

## Камера

Орбитальный режим с двумя подрежимами:

**Follow Mode** (по умолчанию):
- Камера всегда смотрит на маркер игрока
- ПКМ + drag → вращение yaw/pitch вокруг маркера
- Scroll → zoom in/out (8m — 120m)
- Touch: 1 палец = орбита, 2 пальца = zoom + twist

**Free Mode**:
- ЛКМ + drag → пан карты
- ПКМ + drag → вращение
- Scroll → zoom

Pitch ограничен 10°—85° (не даёт камере уйти под землю или в зенит).

---

## Геолокация

Процесс при запуске на телефоне:
1. Запрос разрешения GPS (Android: `Permission.FineLocation`, iOS: автоматически)
2. Ожидание GPS-фикса (до 20 секунд)
3. Конвертация GPS → Unity world coordinates через `GeoReference`
4. Snap позиции к ближайшей точке NavMesh (до 10m)
5. При входе на территорию кампуса → автофокус камеры на игроке

---

## Симуляционная сцена (Benchmark)

Отдельная тестовая сцена для сравнения алгоритмов:

1. Откройте пустую сцену
2. Меню: **CampusNav > Generate Benchmark Scene**
3. Автоматически создаётся:
   - Лабиринтообразный тестовый мир с многоэтажностью
   - 3 предустановленные точки назначения
   - UI с выбором алгоритма и точки
   - Визуализация путей тремя цветами
   - Таблица метрик в реальном времени
4. Нажмите **Bake** на NavMeshSurface
5. Нажмите Play

---

## Что уже сделано

- [x] 7 зданий кампуса (примитивы) с правильными пропорциями
- [x] Коридоры между зданиями (SRK-I↔SRA-I, SRA-I↔SRC мост, SRA-I↔SRK-II мост, SRL-I↔SRL-II)
- [x] Террасный ландшафт (6 уровней, юг выше)
- [x] Тротуары и дорожки
- [x] NavMesh Surface (нужно запечь)
- [x] Орбитальная камера с Follow/Free режимами
- [x] GPS геолокация с запросом разрешений
- [x] Определение границ кампуса
- [x] Автофокус при входе на территорию
- [x] 3 алгоритма поиска пути (NavMesh, A*, Dijkstra)
- [x] Система бенчмарка с таблицей сравнения
- [x] Автогенератор ~180+ RoutePoint'ов (все аудитории, WC, лифты, кафетерия, вендинг, курилки)
- [x] Управление лифтами (вкл/выкл)
- [x] Маркеры: входы, лифты, WC, пандусы, курилки, вендинги, кафетерия
- [x] BuildingRevealer (прозрачность при входе в здание)
- [x] Симуляционная сцена для сравнения алгоритмов

---

## TODO — что осталось сделать вручную

### Критично (без этого не работает навигация)

- [ ] **Открыть Unity**, дождаться компиляции (~1-2 мин)
- [ ] **Меню: CampusNav > Generate Full Campus Scene** → "Создать"
- [ ] **Сохранить сцену** (Ctrl+S)
- [ ] **Запечь NavMesh**: Hierarchy > Campus > NavMeshSurface > Inspector > **Bake**
- [ ] **Меню: CampusNav > Generate All Route Points** (создаст ~180+ ассетов)
- [ ] **Назначить RouteDatabase** на NavigatorManager > CampusNavigator > Route Database
- [ ] **Назначить RouteDatabase** на PathfindingBenchmark > Route Database
- [ ] **Создать GeoReference ассет**: Create > CampusNav > Geo Reference
- [ ] **Назначить GeoReference** в GeoAnchorSystem > Geo Reference

### Симуляционная сцена

- [ ] **Создать новую пустую сцену** (File > New Scene > Empty)
- [ ] **Сохранить как** `Assets/Scenes/BenchmarkScene.unity`
- [ ] **Добавить в Build Settings** (File > Build Settings > Add Open Scenes)
- [ ] **Меню: CampusNav > Generate Benchmark Scene**
- [ ] **Запечь NavMesh**: выбрать BenchmarkNavMesh > Inspector > Bake
- [ ] **Нажать Play** — UI уже подключён, выбирайте точки и алгоритмы

### UI для основной сцены

- [ ] Создать Canvas (GameObject > UI > Canvas)
- [ ] Добавить кнопку "Маршруты" (открытие панели)
- [ ] Добавить панель с:
  - `TMP_InputField` — поиск
  - `TMP_Dropdown` — фильтр по зданию
  - `TMP_Dropdown` — фильтр по категории
  - `ScrollView` с Content для списка кнопок
  - Кнопка-шаблон маршрута (префаб с Button + TextMeshProUGUI)
- [ ] Добавить кнопку "Очистить маршрут"
- [ ] Добавить TextMeshPro для текущего маршрута
- [ ] Повесить `RouteSelectionUI` на Canvas, подключить ссылки
- [ ] Добавить Toggle "Лифты", Toggle "Доступные маршруты"
- [ ] Добавить кнопку "Камера" (Follow ↔ Free)
- [ ] Добавить текст этажа
- [ ] Повесить `NavigationSettingsUI`, подключить ссылки
- [ ] Добавить Dropdown для выбора алгоритма (NavMesh / A* / Dijkstra)

### Мобильный билд

- [ ] File > Build Settings > переключить на **Android** или **iOS**
- [ ] Player Settings > Other Settings:
  - Android: Internet Access = Required, Write Permission = External
  - iOS: добавить `NSLocationWhenInUseUsageDescription` ("CampusNav needs your location to show your position on campus map")
- [ ] Player Settings > разрешить Camera и Location
- [ ] XR Plug-in Management > включить ARCore (Android) / ARKit (iOS)
- [ ] Собрать и протестировать на устройстве

### Калибровка GPS (на месте)

- [ ] Стать у входа SRK-I с телефоном
- [ ] Записать реальные GPS-координаты (через Google Maps или GPS-приложение)
- [ ] Сравнить с `anchorLatitude/anchorLongitude` в GeoReference
- [ ] Подкорректировать `northRotationOffset` (сейчас 25°) — повернуть пока маркер не совпадёт с реальной позицией

### Детализация (не блокирует работу)

- [ ] Заменить примитивы зданий на 3D-модели из Blender/3ds Max (.fbx)
- [ ] Добавить интерьеры зданий (коридоры, двери, лестницы)
- [ ] Добавить ElevatorLink на лифты (NavMeshLink между этажами)
- [ ] Добавить 3D-текстовые метки над зданиями
- [ ] Добавить миникарту
- [ ] Реализовать AR-режим камеры (наложение маршрута на реальный мир)
- [ ] Текстуры и материалы для зданий и территории

---

## Быстрый старт (5 минут)

```
1. Открыть Unity → дождаться компиляции
2. CampusNav > Generate Full Campus Scene → "Создать"
3. Ctrl+S (сохранить сцену)
4. Hierarchy > Campus > NavMeshSurface > Inspector > Bake
5. CampusNav > Generate All Route Points
6. Назначить RouteDatabase в NavigatorManager и PathfindingBenchmark
7. Play → камера следит за Player, можно вращать ПКМ, zoom скроллом
```

Для тестовой сцены:
```
1. File > New Scene > Empty → сохранить как BenchmarkScene
2. CampusNav > Generate Benchmark Scene
3. Hierarchy > BenchmarkNavMesh > Inspector > Bake
4. Play → выбирайте точки и алгоритмы в UI
```
