using System;
using System.IO;
using System.Text.RegularExpressions;
using NXOpen;
using NXOpen.Features;
using NXOpen.UF;
using static NXOpen.Features.BridgeCurveBuilder;
using static NXOpen.UF.UFModl;

namespace FeatureExttraction
{
    public class Class1
    {
        static Session session = Session.GetSession();
        static UFSession ufSession = UFSession.GetUFSession();
        private static Tag lastSelectedTag;
        static ListingWindow lw = session.ListingWindow;
        public static void Main(string[] args)
        {
            UFUi.SelInitFnT selInitFnT = new UFUi.SelInitFnT(selectionParams);
            int scope = UFConstants.UF_UI_SEL_SCOPE_WORK_PART;
            IntPtr intPtr = IntPtr.Zero;
            int outPut;
            double[] position = new double[3];
            ufSession.Ui.SelectWithSingleDialog("Single Dialog Test", "Select Any Face", scope, selInitFnT, intPtr, out outPut, out Tag tag, position, out Tag viewtag);
            if (outPut == UFConstants.UF_UI_OBJECT_SELECTED)
            {
                Tag selectedTag = tag;
                if (lastSelectedTag == selectedTag) // Если выбрана та же грань, что и ранее
                {
                    lastSelectedTag = Tag.Null; // Сбрасываем значение последней выбранной грани
                    lw.WriteLine("Selection canceled by user."); // Сообщаем об отмене выбора пользователем
                }
                else // Если выбрана другая грань
                {
                    lastSelectedTag = selectedTag; // Сохраняем выбранную грань как последнюю выбранную
                    lw.Open();
                    AskFaceData(selectedTag); // Выводим информацию о выбранной грани
                    BSurfInfo(selectedTag);
                }
            }
            else if (outPut == UFConstants.UF_UI_CANCEL) lw.WriteLine("Selection canceled by user.");
        }

        static int selectionParams(IntPtr select, IntPtr userData)
        {
            int num_masks = 2;
            UFUi.Mask[] masks = new UFUi.Mask[2];
            masks[0].object_type = UFConstants.UF_face_type;
            masks[0].object_subtype = 0;
            masks[0].solid_type = 0;
            masks[1].object_type = UFConstants.UF_edge_type;
            masks[1].object_subtype = 0;
            masks[1].solid_type = 0;
            ufSession.Ui.SetSelMask(select, UFUi.SelMaskAction.SelMaskClearAndEnableSpecific, num_masks, masks);
            return UFConstants.UF_UI_SEL_SUCCESS;
        }

        public static void AskFaceData(Tag faceTag)
        {
            double[] position = new double[3];
            double[] dir = new double[3];
            double[] box = new double[6];
            double radius;
            ufSession.Modl.AskFaceData(faceTag, out int type, position, dir, box, out radius, out double rad_datat, out int norm_dir);
            lw.WriteLine(("Type: " + type).Replace(',', '.'));
            lw.WriteLine(("Position: (" + string.Join(" ", position) + ")").Replace(',', '.'));
            lw.WriteLine(("Direction: (" + string.Join(" ", dir) + ")").Replace(',', '.'));
            lw.WriteLine(("Bounding Box: (" + string.Join(" ", box) + ")").Replace(',', '.'));
            lw.WriteLine(("Radius: " + radius).Replace(',', '.'));
            lw.WriteLine(("Radial Data: " + rad_datat).Replace(',', '.'));
            lw.WriteLine(("Normal Direction: " + norm_dir + "\n").Replace(',', '.'));
        }

        public static void BSurfInfo(Tag faceTag)
        {
            TaggedObject taggedObject = NXOpen.Utilities.NXObjectManager.Get(faceTag) as TaggedObject;
            if (taggedObject is Face)
            {
                string tempFilePath = Path.GetTempFileName(); // Создаем временный файл в папке Windows/temp и получаем его путь
                NXObject[] selectedObjects = new NXObject[1] { taggedObject as NXObject };
                lw.Close();
                lw.SelectDevice(ListingWindow.DeviceType.File, tempFilePath); // Информация из ListingWindow будет выводиться в созданный файл
                lw.Open();
                session.Information.DisplayObjectsDetails(selectedObjects); // Вывод информации об объекте
                lw.Close();
                lw.SelectDevice(ListingWindow.DeviceType.Window, ""); // Теперь ListingWindow будет выводить в окно как раньше
                lw.Open();
                string content = File.ReadAllText(tempFilePath); // Читаем файл
                Regex regex = new Regex("^Surface Type.*", RegexOptions.Multiline); // Регулярное выражение для поиска нужной строки

                lw.WriteLine(regex.Match(content).Value); // Выводим результат регулярного выражения
                lw.WriteLine("BSurf information:\n");
                File.Delete(tempFilePath); // Удаляем временный файл

                Face face = (Face)taggedObject;
                Part workPart = session.Parts.Work;
                IForm nullFeatIForm = null;
                IFormBuilder iFormBuilder1 = workPart.Features.CreateIformBuilder(nullFeatIForm);
                iFormBuilder1.ParameterDirection = IFormBuilder.ParameterDirectionOptions.IsoV; // Направление сплайнов: IsoV - горизонтальное (вдоль грани), IsoU - вертикальное (поперек грани)
                iFormBuilder1.Number = 5; // Кол-во сплайнов
                iFormBuilder1.CurveShaper.Number = 10; // Кол-во точек на сплайне
                Face[] faces1 = { face };
                FaceDumbRule faceDumbRule = workPart.ScRuleFactory.CreateRuleFaceDumb(faces1);
                SelectionIntentRule[] rules2 = { faceDumbRule };
                iFormBuilder1.FaceToDeform.FaceCollector.ReplaceRules(rules2, false);
                iFormBuilder1.Commit();

                Bsurface bsurf = new Bsurface();
                ufSession.Modl.AskBsurf(faceTag, out bsurf);
                lw.WriteLine("Control polygon poles:\n");
                for (int i = 0; i < bsurf.num_poles_u * bsurf.num_poles_v; i++) lw.WriteLine(("Pole " + i + ": (" + bsurf.poles[i, 0] + " " + bsurf.poles[i, 1] + " " + bsurf.poles[i, 2] + " " + bsurf.poles[i, 3] + ")").Replace(',', '.'));
                lw.WriteLine("\nKnots_u:\n");
                for (int i = 0; i < bsurf.num_poles_u; i++) lw.WriteLine(("Knot_u " + i + ": " + bsurf.knots_u[i]).Replace(',', '.'));
                lw.WriteLine("\nKnots_v:\n");
                for (int i = 0; i < bsurf.num_poles_v; i++) lw.WriteLine(("Knot v " + i + ": " + bsurf.knots_v[i]).Replace(',', '.'));
            }
            else lw.WriteLine("Selected tag is not a face.");
        }

        public static int GetUnloadOption(string arg)
        {
            return System.Convert.ToInt32(Session.LibraryUnloadOption.Immediately);
        }
    }
}