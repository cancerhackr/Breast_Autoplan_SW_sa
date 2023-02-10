using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;

using System.Windows;

using ScriptContext = Testing.Context;
using Application = VMS.TPS.Common.Model.API.Application;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace Breast_Autoplan_SW_sa
{
    class Program
    {
        [STAThread]
        static void Main(/*string[] args*/)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static void Execute(Application app)
        {

            #region Initialize for debugging

            string patientID = "BREAST_IK_SW_Prospective5";
            string courseID = "V1.2";
            string planID = "BREL1";

            Patient patient = null;
            ScriptContext context;
            Window window;

            Course course;

            try
            {
                patient = app.OpenPatientById(patientID);
                course = patient.Courses.First();
                ExternalPlanSetup plan = course.ExternalPlanSetups.First(x => x.Id.ToLower() == planID.ToLower());

                context = new ScriptContext()
                {
                    Course = patient.Courses.First(x => x.Id == courseID),
                    Patient = patient,
                    PlanSetup = plan,
                    StructureSet = plan.StructureSet
                };
                window = new Window();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return;
            }

            #endregion

            try
            {
                #region This is the actual script bits

                #region Checking if the context loaded properly

                if (context.Patient == null)
                {
                    _ = MessageBox.Show("Please load a valid patient before running this script.");
                    window.Loaded += Window_Loaded;
                    return;
                }

                patient = context.Patient;

                if (patient.Courses.Count() == 0) //  If there are no courses of treatment available, let the user know and exit.
                {
                    _ = MessageBox.Show("Please load a valid course before running this script.");
                    window.Loaded += Window_Loaded;
                    return;
                }

                course = context.Course;

                if (course == null) //  If they didn't, this is where things get interesting; how should this be handled? Another drop-down in the UserControl
                                    //  would allow the users to pick the course, and if there's only one it can be selected for them.
                {
                    List<string> courseIDs = new List<string>();
                    foreach (Course course2 in patient.Courses)
                    {
                        courseIDs.Add(course2.Id);
                    }
                    string crs = string.Join("\n", courseIDs.ToArray());
                    _ = MessageBox.Show($"Courses are available, but you didn't choose one!\n\n{crs}\n\nExiting...");
                    window.Loaded += Window_Loaded;
                    return;
                }

                try
                {
                    if (course.PlanSetups.Count() == 0)
                    {
                        MessageBox.Show("Warning: no plan in the course is loaded/available.");
                        window.Loaded += Window_Loaded;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading Plan Setup: {ex.Message}");
                    window.Loaded += Window_Loaded;
                    return;
                }

                //  JAK: probably not needed, but Irfan kept it for some reason, so I'll give him the benefit
                //  of the doubt.  It's not like it takes up a lot of computing resources, but I'll ask him
                //  when he gets back.
                if (context.PlanSetup == null)
                {
                    MessageBox.Show("Please load the Breast plan created by CT");
                    window.Loaded += Window_Loaded;
                    return;
                }

                //  This script only works on POP plans (i.e., 2 beams; no more, no less).
                if (context.PlanSetup.Beams.Count(x => !x.IsSetupField) != 2)
                {
                    MessageBox.Show("This code is only works for two POP tangents.\n Please check and try again");
                    window.Loaded += Window_Loaded;
                    return;
                }

                //  Ensure there's an external contour.
                IEnumerable<Structure> structures = context.ExternalPlanSetup.StructureSet.Structures;
                Structure external = structures.FirstOrDefault(x => x.Id.ToUpper().Contains("BODY"));
                if (external == null)
                {
                    external = structures.FirstOrDefault(x => x.DicomType.ToUpper() == "EXTERNAL");
                }
                if (external == null)
                {
                    _ = MessageBox.Show("External structure can't be found!  Exiting...");
                    window.Loaded += Window_Loaded;
                    return;
                }

                #endregion

                #region Creating user control window

                Breast_Autoplan_SW userControl = new Breast_Autoplan_SW(context);
                window.Content = userControl;
                window.SizeToContent = SizeToContent.WidthAndHeight;
                window.ResizeMode = ResizeMode.CanMinimize;

                #endregion

                #endregion

                _ = window.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (patient != null)
                {
                    //app.SaveModifications();  //  We don't want to actually apply the modifications to the database
                    app.ClosePatient();
                }
            }
        }

        // Rest of the code in Breast_Autoplan_SW.xaml.cs
        #region UserControl window closing function on error

        //Window (i.e. GUI) loading event function created to close window if no patient and/or course is loaded
        private static void Window_Loaded(object sender, EventArgs e)
        {
            Window window = sender as Window;
            window.Close();
        }

        #endregion

    }
}
