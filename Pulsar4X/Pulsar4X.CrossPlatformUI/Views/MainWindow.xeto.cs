using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;

using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Views {
	public class MainWindow : Panel {
		#region Form Controls
		protected StackLayout TopButtonBar;

		#region Main Navigation Buttons
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

		protected StackLayout adv_buttons;
		#region Advance Time Buttons
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
		#endregion

		protected DropDown dd_subpulse;
		protected TabControl view_tabs;
		#endregion

		private GameVM _game;
		public MainWindow(GameVM game) {
			_game = game;
			XamlReader.Load(this);
			//SetupAllTheButtons();
		}
		
		public void AddOrSelectTabPanel(string title, Container c, bool activate = true, bool force_new = false) {
            if (!force_new) {
                foreach (TabPage p in view_tabs.Pages) {
                    if (p.Text == title) {
                        if (activate) { view_tabs.SelectedPage = p; }
                        return;
                    }
                }
            }
			TabPage tp = new TabPage();
			tp.Content = c;
			tp.Text = title;
			view_tabs.Pages.Add(tp);
            if (activate) { view_tabs.SelectedPage = tp; }
		}

		private void SetupAllTheButtons() {
			Func<string,string> resource_for = s => $"Pulsar4X.CrossPlatformUI.Resources.{s}";

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
		}

		#region Navigation Button Click Handlers
		/*
		private void btn_system_map_Click(object sender, EventArgs e) {
			TabPage tp = new TabPage();
			tp.Text = "System Map";
			tp.Content = new SystemView(_game);
			view_tabs.Pages.Add(tp);
		}
		private void btn_system_info_Click(object sender, EventArgs e) {

		}

		private void btn_industry_Click(object sender, EventArgs e) {

		}

		private void btn_research_Click(object sender, EventArgs e) {

		}

		private void btn_wealth_Click(object sender, EventArgs e) {

		}

		private void btn_ground_units_Click(object sender, EventArgs e) {

		}

		private void btn_commanders_Click(object sender, EventArgs e) {

		}

		private void btn_class_design_Click(object sender, EventArgs e) {

		}

		private void btn_unit_details_Click(object sender, EventArgs e) {

		}

		private void btn_fleet_orders_Click(object sender, EventArgs e) {

		}

		private void btn_task_forces_Click(object sender, EventArgs e) {

		}

		private void btn_fighters_Click(object sender, EventArgs e) {

		}

		private void btn_combat_assignments_Click(object sender, EventArgs e) {

		}

		private void btn_system_display_Click(object sender, EventArgs e) {

		}

		private void btn_galactic_map_Click(object sender, EventArgs e) {

		}

		private void btn_races_Click(object sender, EventArgs e) {

		}

		private void btn_research_project_Click(object sender, EventArgs e) {

		}

		private void btn_available_colonies_Click(object sender, EventArgs e) {

		}

		private void btn_fuel_report_Click(object sender, EventArgs e) {

		}

		private void btn_damaged_ships_Click(object sender, EventArgs e) {

		}

		private void btn_production_overview_Click(object sender, EventArgs e) {

		}

		private void btn_tech_report_Click(object sender, EventArgs e) {

		}

		private void btn_geo_survey_report_Click(object sender, EventArgs e) {

		}

		private void btn_shipping_lines_Click(object sender, EventArgs e) {

		}

		private void btn_sector_assignment_Click(object sender, EventArgs e) {

		}

		private void btn_refresh_Click(object sender, EventArgs e) {

		}

		private void btn_events_Click(object sender, EventArgs e) {

		}

		private void btn_intelligence_Click(object sender, EventArgs e) {

		}
		*/
		#endregion
	}
}
