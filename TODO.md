# CampusNav -- что осталось сделать

## 1. Сгенерировать 3D-сцену
- [x] Открыть Unity, дождаться компиляции скриптов (~1-2 мин, AR пакеты загрузятся автоматически)
- [x] Меню **CampusNav > Generate Full Campus Scene** > нажать "Создать"
- [x] Сохранить сцену (Ctrl+S)

## 2. Запечь NavMesh
- [x] В Hierarchy выбрать **Campus > NavMeshSurface**
- [x] В Inspector нажать **Bake**
- [x] Проверить: нажать Play, камера должна следить за Player

## 3. Создать базу маршрутов
- [x] Project > ПКМ > Create > **CampusNav > Route Database** -- создать ассет
- [x] Меню **CampusNav > Generate All Route Points** -- автогенерация ~180+ точек
- [ ] В Inspector на **NavigatorManager** назначить Route Database в поле CampusNavigator
- [ ] В Inspector на **PathfindingBenchmark** назначить Route Database

## 4. Собрать UI
- [ ] Создать Canvas (GameObject > UI > Canvas)
- [ ] Добавить кнопку "Маршруты" (открытие панели)
- [ ] Добавить панель с:
  - TMP_InputField (поиск)
  - TMP_Dropdown (фильтр по зданию)
  - TMP_Dropdown (фильтр по категории)
  - ScrollView с Content для списка кнопок
  - Кнопка-шаблон маршрута (префаб)
- [ ] Добавить кнопку "Очистить маршрут"
- [ ] Добавить TextMeshPro для текущего маршрута
- [ ] Повесить **RouteSelectionUI** на Canvas, подключить все ссылки
- [ ] Добавить Toggle "Лифты", Toggle "Доступные маршруты", кнопку "Камера", текст этажа
- [ ] Повесить **NavigationSettingsUI**, подключить ссылки
- [ ] Добавить Dropdown для выбора алгоритма (NavMesh / A* / Dijkstra)

## 5. Настроить геолокацию
- [x] GeoReference обновлен — GPS-координаты VGTU Saulėtekio Rūmai
- [x] Границы кампуса установлены в GeoAnchorSystem
- [x] Android permission request добавлен
- [x] Авто-фокус камеры при входе на территорию кампуса
- [ ] Создать ассет Create > **CampusNav > Geo Reference** и назначить в GeoAnchorSystem
- [ ] Проверить northRotationOffset (25°) — может потребоваться калибровка на месте

## 6. Настроить билд под мобильные + AR
- [ ] File > Build Settings > переключить на Android или iOS
- [ ] Player Settings > разрешить Camera и Location
- [ ] XR Plug-in Management > включить ARCore (Android) или ARKit (iOS)
- [ ] Проверить что Info.plist содержит NSLocationWhenInUseUsageDescription (iOS)
- [ ] Собрать и протестировать на устройстве

## 7. Запустить бенчмарк алгоритмов
- [ ] В Play mode вызвать PathfindingBenchmark.RunFullBenchmark() через Inspector кнопку
- [ ] Проверить Console — таблица сравнения NavMesh vs A* vs Dijkstra
- [ ] Включить Gizmos в Scene View для визуализации путей разными цветами

## 8. Камера — орбитальный режим ✅
- [x] Follow mode: камера вращается вокруг маркера игрока
- [x] Free mode: свободное перемещение + вращение
- [x] Touch: 1 палец = орбита/пан, 2 пальца = zoom + yaw + pitch
- [x] Scroll wheel zoom
- [x] Ограничение pitch (10°-85°)

## 9. Не-код задачи (ручная работа)
- [ ] Получить планы зданий (из администрации/деканата)
- [ ] Отмоделировать детальные корпуса в Blender/3ds Max (заменить примитивы)
- [ ] Импортировать модели в Unity (.fbx), заменить здания в сцене
- [ ] Подкорректировать позиции зданий если нужно (двигать в Scene view)
- [ ] Добавить интерьеры зданий (коридоры внутри, двери, лестницы)

## 10. На будущее
- [ ] Добавить ElevatorLink на лифты (когда будут многоэтажные модели с интерьерами)
- [ ] Настроить NavMeshLink между этажами
- [ ] Добавить 3D-текстовые метки над зданиями
- [ ] Добавить миникарту
- [ ] Реализовать AR-режим камеры (наложение маршрута на реальный мир)
