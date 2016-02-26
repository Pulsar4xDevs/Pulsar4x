using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using OpenTK;
using OpenTK.Graphics;
using Pulsar4X.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views {
	public class SystemView : Panel {
		#region Window Controls

		protected PixelLayout PLayout;
		protected StackLayout MapOverlay;
		protected StackLayout TopButtonBar;
		protected ListBox Systems;
		protected RenderCanvas RenderCanvas;
		private UITimer timDraw;

		#region Oh God Buttons

		protected StackLayout adv_buttons;
		protected StackLayout pulse_buttons;


		#region Top Buttons
		protected Button btn_system_info;
		protected Button btn_industry;
		protected Button btn_research;
		protected Button btn_wealth;
		protected Button btn_ground_units;
		protected Button btn_commanders;
		protected Button btn_class_design;
		protected Button btn_unit_details;
		protected Button btn_fleet_orders;
		protected Button btn_task_forces;
		protected Button btn_fighters;
		protected Button btn_combat_assignments;
		protected Button btn_system_display;
		protected Button btn_galactic_map;
		protected Button btn_races;
		protected Button btn_research_project;
		protected Button btn_available_colonies;
		protected Button btn_fuel_report;
		protected Button btn_damaged_ships;
		protected Button btn_production_overview;
		protected Button btn_tech_report;
		protected Button btn_geo_survey_report;
		protected Button btn_shipping_lines;
		protected Button btn_sector_assignment;
		protected Button btn_refresh;
		protected Button btn_events;
		protected Button btn_intelligence;
		#endregion

		#region Map Movement Buttons
		protected Button btn_up;
		protected Button btn_down;
		protected Button btn_left;
		protected Button btn_right;
		protected Button btn_zoom_in;
		protected Button btn_zoom_out;
		protected Button btn_min_zoom;
		#endregion

		#region Timing Buttons
		protected Button btn_time_5sec;
		protected Button btn_time_30sec;
		protected Button btn_time_2min;
		protected Button btn_time_5min;
		protected Button btn_time_20min;
		protected Button btn_time_1hr;
		protected Button btn_time_3hrs;
		protected Button btn_time_8hrs;
		protected Button btn_time_1day;
		protected Button btn_time_5day;
		protected Button btn_time_30day;

		protected Button btn_pulse_auto;
		protected Button btn_pulse_5sec;
		protected Button btn_pulse_30sec;
		protected Button btn_pulse_2min;
		protected Button btn_pulse_5min;
		protected Button btn_pulse_20min;
		protected Button btn_pulse_1hr;
		protected Button btn_pulse_2hrs;
		protected Button btn_pulse_6hrs;
		protected Button btn_pulse_1day;
		protected TextBox tb_current_pulse_length;
		#endregion

		#endregion

		#endregion

		#region ViewModels
		protected SystemVM CurrentSystem;
		protected RenderVM RenderVM;
		#endregion

		protected Bitmap WealthBmp;

		private bool mouse_held = false;
		private bool continue_drag = false;
		private Vector2 mouse_held_position;
		private Vector2 mouse_released_position;
		private const float mouse_move_threshold = 20f;
		
		private OpenGLRenderer Renderer;

		public SystemView(GameVM GameVM) {
			WealthBmp = Bitmap.FromResource("Pulsar4X.CrossPlatformUI.Resources.AuroraButtons.wealth.png");
			RenderVM = new RenderVM();
			Renderer = new OpenGLRenderer(RenderVM);
			DataContext = GameVM;
			RenderCanvas = new RenderCanvas(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
			XamlReader.Load(this);

			SetupAllTheButtons();
			
			//Systems.BindDataContext(s => s.DataStore, (GameVM g) => g.StarSystems);
			//Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
			//Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());

			//direct binding - might need to be replaced later
			//Systems.Bind(s => s.SelectedValue, RenderVM, (RenderVM rvm) => rvm.ActiveSystem);

			RenderCanvas.GLInitalized += Initialize;
			RenderCanvas.GLDrawNow += DrawNow;
			RenderCanvas.GLShuttingDown += Teardown;
			RenderCanvas.GLResize += Resize;
			RenderCanvas.MouseMove += WhenMouseMove;
			RenderCanvas.MouseDown += WhenMouseDown;
			RenderCanvas.MouseUp += WhenMouseUp;
			RenderCanvas.MouseWheel += WhenMouseWheel;
			RenderCanvas.MouseLeave += WhenMouseLeave;
			//SizeChanged += (s, e) => {
			//	PLayout.Size = ClientSize;
			//	RenderCanvas.Size = ClientSize;
			//	MapOverlay.Size = ClientSize;
			//	RenderVM.drawPending = true;
			//	PLayout.RemoveAll();
			//	PLayout.Add(RenderCanvas, 0, 0);
			//	PLayout.Add(MapOverlay, 0, 0);
			//};


		}

		private void WhenMouseLeave(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = false;
			continue_drag = false;
		}

		private void WhenMouseWheel(object sender, MouseEventArgs e) {
			e.Handled = true;
			RenderVM.UpdateCameraZoom((int)e.Delta.Height);
		}

		private void WhenMouseUp(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = false;
			continue_drag = false;
		}

		private void WhenMouseDown(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = true;
			mouse_held_position.X = e.Location.X;
			mouse_held_position.Y = e.Location.Y;
		}

		private void WhenMouseMove(object sender, MouseEventArgs e) {
			e.Handled = true;
			if (mouse_held) {
				Vector2 mouse_pos = new Vector2(e.Location.X, e.Location.Y);
				var delta = mouse_pos - mouse_held_position;
				if (delta.Length > mouse_move_threshold || continue_drag) {
					continue_drag = true;
					RenderVM.UpdateCameraPosition(delta);
					mouse_held_position = mouse_pos;
				}
			}
		}

		private void Draw() {
			RenderVM.drawPending = true;
		}

		public void Initialize(object sender, EventArgs e) {
			RenderCanvas.MakeCurrent();
			var bounds = RenderCanvas.Bounds;
			Renderer.Initialize(bounds.X, bounds.Y, bounds.Width, bounds.Height);

			//we need this to run on its own because we cant have rendering blocked by the
			//the rest of the system or waiting for an advance time command
			timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
			timDraw.Elapsed += timDraw_Elapsed;
			timDraw.Start();
		}

		private void timDraw_Elapsed(object sender, EventArgs e) {
			if (!RenderVM.drawPending || !RenderCanvas.IsInitialized) {
				return;
			}

			RenderCanvas.MakeCurrent();

			Renderer.Draw(RenderVM);

			RenderCanvas.SwapBuffers();

			RenderVM.drawPending = false;
		}

		public void DrawNow(object sender, EventArgs e) {
			Draw();
			
		}

		public void Resize(object sender, EventArgs e) {
			RenderCanvas.MakeCurrent();
			var bounds = RenderCanvas.Bounds;
			RenderVM.Resize(bounds.Width, bounds.Height);
			Renderer.Resize(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			RenderVM.drawPending = true;
		}

		public void Teardown(object sender, EventArgs e) {
			Renderer.Destroy();
		}

		private string resource_for(string name) { return $"Pulsar4X.CrossPlatformUI.Resources.{name}"; }

		private void SetupAllTheButtons() {
			#region Set Topbar Button Images
			btn_system_info.Image = Bitmap.FromResource(resource_for("AuroraButtons.system_info.png"));
			btn_system_info.Size = btn_system_info.Image.Size;

			btn_industry.Image = Bitmap.FromResource(resource_for("AuroraButtons.industry.png"));
			btn_industry.Size = btn_industry.Image.Size;

			btn_research.Image = Bitmap.FromResource(resource_for("AuroraButtons.research.png"));
			btn_research.Size = btn_research.Image.Size;

			btn_wealth.Image = Bitmap.FromResource(resource_for("AuroraButtons.wealth.png"));
			btn_wealth.Size = btn_wealth.Image.Size;

			btn_ground_units.Image = Bitmap.FromResource(resource_for("AuroraButtons.ground_units.png"));
			btn_ground_units.Size = btn_ground_units.Image.Size;

			btn_commanders.Image = Bitmap.FromResource(resource_for("AuroraButtons.commanders.png"));
			btn_commanders.Size = btn_commanders.Image.Size;

			btn_class_design.Image = Bitmap.FromResource(resource_for("AuroraButtons.class_design.png"));
			btn_class_design.Size = btn_class_design.Image.Size;

			btn_unit_details.Image = Bitmap.FromResource(resource_for("AuroraButtons.unit_details.png"));
			btn_unit_details.Size = btn_unit_details.Image.Size;

			btn_fleet_orders.Image = Bitmap.FromResource(resource_for("AuroraButtons.fleet_orders.png"));
			btn_fleet_orders.Size = btn_fleet_orders.Image.Size;

			btn_task_forces.Image = Bitmap.FromResource(resource_for("AuroraButtons.task_forces.png"));
			btn_task_forces.Size = btn_task_forces.Image.Size;

			btn_fighters.Image = Bitmap.FromResource(resource_for("AuroraButtons.fighters.png"));
			btn_fighters.Size = btn_fighters.Image.Size;

			btn_combat_assignments.Image = Bitmap.FromResource(resource_for("AuroraButtons.combat_assignments.png"));
			btn_combat_assignments.Size = btn_combat_assignments.Image.Size;

			btn_system_display.Image = Bitmap.FromResource(resource_for("AuroraButtons.system_display.png"));
			btn_system_display.Size = btn_system_display.Image.Size;

			btn_galactic_map.Image = Bitmap.FromResource(resource_for("AuroraButtons.galactic_map.png"));
			btn_galactic_map.Size = btn_galactic_map.Image.Size;

			btn_races.Image = Bitmap.FromResource(resource_for("AuroraButtons.races.png"));
			btn_races.Size = btn_races.Image.Size;

			btn_research_project.Image = Bitmap.FromResource(resource_for("AuroraButtons.research_project.png"));
			btn_research_project.Size = btn_research_project.Image.Size;

			btn_available_colonies.Image = Bitmap.FromResource(resource_for("AuroraButtons.available_colonies.png"));
			btn_available_colonies.Size = btn_available_colonies.Image.Size;

			btn_fuel_report.Image = Bitmap.FromResource(resource_for("AuroraButtons.fuel_report.png"));
			btn_fuel_report.Size = btn_fuel_report.Image.Size;

			btn_damaged_ships.Image = Bitmap.FromResource(resource_for("AuroraButtons.damaged_ships.png"));
			btn_damaged_ships.Size = btn_damaged_ships.Image.Size;

			btn_production_overview.Image = Bitmap.FromResource(resource_for("AuroraButtons.production_overview.png"));
			btn_production_overview.Size = btn_production_overview.Image.Size;

			btn_tech_report.Image = Bitmap.FromResource(resource_for("AuroraButtons.tech_report.png"));
			btn_tech_report.Size = btn_tech_report.Image.Size;

			btn_geo_survey_report.Image = Bitmap.FromResource(resource_for("AuroraButtons.geo_survey_report.png"));
			btn_geo_survey_report.Size = btn_geo_survey_report.Image.Size;

			btn_shipping_lines.Image = Bitmap.FromResource(resource_for("AuroraButtons.shipping_lines.png"));
			btn_shipping_lines.Size = btn_shipping_lines.Image.Size;

			btn_sector_assignment.Image = Bitmap.FromResource(resource_for("AuroraButtons.sector_assignment.png"));
			btn_sector_assignment.Size = btn_sector_assignment.Image.Size;

			btn_refresh.Image = Bitmap.FromResource(resource_for("AuroraButtons.refresh.png"));
			btn_refresh.Size = btn_refresh.Image.Size;

			btn_events.Image = Bitmap.FromResource(resource_for("AuroraButtons.events.png"));
			btn_events.Size = btn_events.Image.Size;

			btn_intelligence.Image = Bitmap.FromResource(resource_for("AuroraButtons.intelligence.png"));
			btn_intelligence.Size = btn_intelligence.Image.Size;
			#endregion

			#region Set move button images
			btn_up.Image = Bitmap.FromResource(resource_for("AuroraButtons.up.png"));
			btn_up.Size = btn_up.Image.Size;

			btn_down.Image = Bitmap.FromResource(resource_for("AuroraButtons.down.png"));
			btn_down.Size = btn_down.Image.Size;

			btn_left.Image = Bitmap.FromResource(resource_for("AuroraButtons.left.png"));
			btn_left.Size = btn_left.Image.Size;

			btn_right.Image = Bitmap.FromResource(resource_for("AuroraButtons.right.png"));
			btn_right.Size = btn_right.Image.Size;

			btn_zoom_in.Image = Bitmap.FromResource(resource_for("AuroraButtons.zoom_in.png"));
			btn_zoom_in.Size = btn_zoom_in.Image.Size;

			btn_zoom_out.Image = Bitmap.FromResource(resource_for("AuroraButtons.zoom_out.png"));
			btn_zoom_out.Size = btn_zoom_out.Image.Size;
			#endregion

			Eto.Style.Add<Button>("green-background", btn => {
				btn.BackgroundColor = Color.FromArgb(0, 0x99, 0);
				btn.Font = new Font(btn.Font.Family, btn.Font.Size, FontStyle.Bold);
			});
			foreach (Button btn in adv_buttons.Children.OfType<Button>()) { btn.Style = "green-background"; }
			foreach (Button btn in pulse_buttons.Children.OfType<Button>()) { btn.Style = "green-background"; }
			btn_min_zoom.Style = "green-background";
		}
	}
}

