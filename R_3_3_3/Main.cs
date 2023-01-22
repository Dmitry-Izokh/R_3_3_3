using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R_3_3_3
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        /*public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var selectedRef = uidoc.Selection.PickObject(ObjectType.Element, "Выберите элемент");
            var selectedElement = doc.GetElement(selectedRef);

            if (selectedElement is FamilyInstance)
            {
                using(Transaction ts =new Transaction(doc, "Задайте параметр"))
                {
                    ts.Start();
                    var familyInstance = selectedElement as FamilyInstance;
                    Parameter commentParameter = familyInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    commentParameter.Set("Тестовая запись параметра");
                   
                    Parameter typeCommentParameter = familyInstance.Symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS);
                    commentParameter.Set("Тестовая запись параметра");

                    ts.Commit();
                }    
            }
             
            return Result.Succeeded;
        }
        */

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Код создания параметра
            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
            using (Transaction ts = new Transaction(doc, "Задайте параметр"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Длинна труб с учетом коэф 1.1", categorySet, BuiltInParameterGroup.PG_DATA, true);

                ts.Commit();
            }

            //Код заполнения нового параметра
                IList<Pipe> pipeList = null;            
                pipeList = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .ToList();

                using (Transaction ts = new Transaction(doc, "Задайте параметр"))
                {
                    ts.Start();
                    foreach (var pipeInstance in pipeList)
                    {
                        if (pipeInstance is Pipe)
                        {
                            Parameter oldlenhthParametr = pipeInstance.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                            if (oldlenhthParametr.StorageType == StorageType.Double)
                            {
                                double lengthValue = UnitUtils.ConvertFromInternalUnits(oldlenhthParametr.AsDouble(), /*UnitTypeId.Meters*/ DisplayUnitType.DUT_METERS);
                                double length = /*oldlenhthParametr*/lengthValue/*.AsDouble()*/ * 1.1;
                                Parameter newlenhthParametr = pipeInstance.LookupParameter("Длинна труб с учетом коэф 1.1");
                                newlenhthParametr.Set($"{length}");
                            }
                        }
                    }

                    ts.Commit();
                }          
                
            return Result.Succeeded;
        }

        // Метод для кода создания параметра
            private void CreateSharedParameter(Application application, Document doc,
                string parameterName, CategorySet categorySet,
                BuiltInParameterGroup builtInParameterGroup, bool isInstance)
            {
                DefinitionFile definitionFile = application.OpenSharedParameterFile();

                if (definitionFile == null)
                {
                    TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                    return;
                }
                Definition definition = definitionFile.Groups.SelectMany(group => group.Definitions)
                    .FirstOrDefault(def => def.Name.Equals(parameterName));
                if (definition == null)
                {
                    TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                    return;
                }

                Binding binding = application.Create.NewTypeBinding(categorySet);
                if (isInstance)
                    binding = application.Create.NewInstanceBinding(categorySet);
                BindingMap map = doc.ParameterBindings;
                map.Insert(definition, binding, builtInParameterGroup);
            }

        }
    }



