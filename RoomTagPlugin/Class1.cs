using Autodesk.Revit.ApplicationServices;
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

namespace RoomTagPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class RoomTagCreation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View v = doc.ActiveView;
            FilteredElementCollector FEC = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms);
            IList<ElementId> roomids = FEC.ToElementIds() as IList<ElementId>;

            Parameter param = v.get_Parameter(BuiltInParameter.VIEW_PHASE);
            ElementId eID = param.AsElementId();
            Phase newPhase = doc.GetElement(eID) as Phase;
            Room newRoom = doc.Create.NewRoom(newPhase);


                View view = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .OfType<View>()
                .Where(x => !x.IsTemplate)
                .FirstOrDefault();
               List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

               Level level1 = listLevel
                .FirstOrDefault();

               PlanCircuit planCircuit = null;

               PlanTopology planTopology = doc.get_PlanTopology(level1);

                foreach (PlanCircuit circuit in planTopology.Circuits)
                {

                   if (null != circuit)
                   {
                    planCircuit = circuit;
                    break;
                   }
                }
           
            Transaction transaction = new Transaction(doc);
                transaction.Start("Создание помещений и марок");
                doc.Create.NewRoom(newRoom, planCircuit);
            foreach (ElementId roomid in roomids)
            {
                Element e = doc.GetElement(roomid);
                Room r = e as Room;
                XYZ cen = GetRoomCenter(r);
                UV center = new UV(cen.X, cen.Y);
                doc.Create.NewRoomTag(new LinkElementId(roomid), center, v.Id);
            }
                transaction.Commit();


            return Result.Succeeded;
        }
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
        public XYZ GetRoomCenter(Room room)
        {
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            return roomCenter;
        }
        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
        }

    }

}
       