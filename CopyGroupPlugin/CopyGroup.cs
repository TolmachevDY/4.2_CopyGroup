using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //обращаемся к текущему документу
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                //получаем ссылку на группу
                GroupPickFilter groupPickFilter = new GroupPickFilter();//создаем экземпляр класса фильтра
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберете группу объектов");
                Element element = doc.GetElement(reference);//преобразуем в тип Element
                Group group = element as Group;//преобразуем элемент в группу

                //находим точку середины выбранной группы
                XYZ groupCenter = GetElementCenter(group);
                //находим комнату к которой принадлежит точка центра исходной группы
                Room room = GetRoomByPoint(doc, groupCenter);
                //находим центр данной комнаты
                XYZ roomCenter = GetElementCenter(room);
                //находим смещение центра группы относительно центра комнаты
                XYZ offset = groupCenter - roomCenter;


                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");//выбираем точку куда будем копировать группу
                //определяем комнату, которй принадлежит выбранная точка
                Room room1 = GetRoomByPoint(doc, point);
                //находим центр найденой комнаты
                XYZ roomCenter1 = GetElementCenter(room1);
                //находим точку вставки новой группы
                XYZ newGroupCenter = roomCenter1 + offset;

                //создаем транзакцию
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(newGroupCenter, group.GroupType);//создаем новую группу в указанной точке
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)//обработка исключения при нажатии Esc
            {
                return Result.Cancelled;
            }

            catch (Exception ex)//обработка остальных исключений
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)//метод для определения центра элемента
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)//метод для определения комнаты по точке, выбранной в ней
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);//выбираем все комнаты в дркументе по категории
            foreach (Element e in collector)
            {
                Room room = e as Room;//преобразуем выбранный элемент в элемент типа комната
                if (room != null)
                {
                    if (room.IsPointInRoom(point))//если точка принадлежит комнате, то вызвращаем данный элемент в виде результата
                    {
                        return room;
                    }
                }
            }
            return null;//если не нашли комнату к которой принадлежит точка
        }
    }

    public class GroupPickFilter : ISelectionFilter//класс для фильтрации элементов выбора (будут подсвечиваться только нужные элементы)
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)//проверяем по id категории
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
