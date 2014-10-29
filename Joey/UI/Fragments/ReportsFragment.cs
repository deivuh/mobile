﻿using System;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toggl.Phoebe.Data.Reports;
using Toggl.Phoebe.Data;
using System.Threading.Tasks;
using XPlatUtils;
using Toggl.Phoebe.Net;
using Android.Graphics;
using ListFragment = Android.Support.V4.App.ListFragment;
using Android.Widget;
using System.Collections.Generic;
using Toggl.Joey.UI.Utils;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : ListFragment
    {
        private BarChart barChart;
        private PieChart pieChart;
        private TextView timePeriod;
        private TextView totalValue;
        private TextView billableValue;
        private ImageButton previousPeriod;
        private ImageButton nextPeriod;
        private SummaryReportView summaryReport;
        private int backDate;

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);

            barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);

            timePeriod = view.FindViewById<TextView> (Resource.Id.TimePeriodLabel);
            totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
            billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

            previousPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonPrevious);
            nextPeriod = view.FindViewById<ImageButton> (Resource.Id.ButtonNext);

            previousPeriod.Click += (sender, e) => NavigatePeriod (1);
            nextPeriod.Click += (sender, e) => NavigatePeriod (-1);

            pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
            var listener = new SliceListener ();
            pieChart.SetOnSliceClickedListener (listener);
            pieChart.SliceClicked += OnSliceSelect; 

            LoadElements ();
            return view;
        }

        private void NavigatePeriod (int direction)
        {
            if (backDate == 0 && direction < 0) {
                backDate = 0;
            } else {
                backDate = backDate + direction;
            }
            LoadElements ();
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            pieChart.SelectSlice (position);
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (position);
            if (adapter == null) {
                return;
            }

            var model = adapter.GetItem (position);
            if (model == null) {
                return;
            }
        }

        private void OnSliceSelect (int position)
        {
            var adapter = ListView.Adapter as ProjectListAdapter;
            adapter.SetFocus (position);
        }

        private void EnsureAdapter ()
        {
            var adapter = new ProjectListAdapter (summaryReport.Projects);
            ListAdapter = adapter;
        }

        private async void LoadElements ()
        {
            await LoadData ();
            timePeriod.Text = FormattedDateSelector ();
            totalValue.Text = summaryReport.TotalGrand;
            billableValue.Text = summaryReport.TotalBillale;
            EnsureAdapter ();
            ListViewHeight (ListView);
            GeneratePieChart ();
            GenerateBarChart ();
        }

        private void GenerateBarChart ()
        {
            barChart.Reset ();
            foreach (var p in summaryReport.Activity) {
                var d = new BarItem ();
                d.Value = (float)p.TotalTime;
                d.Name = p.StartTime.ToShortTimeString ();
                d.Color = Color.ParseColor ("#00AEFF");
                barChart.AddBar (d);
            }
            barChart.CeilingValue = summaryReport.GetCeilingSeconds ();
            barChart.SetBarTitles (summaryReport.ChartRowLabels ());
            barChart.SetLineTitles (summaryReport.ChartTimeLabels ());
            barChart.Refresh ();
        }

        private void GeneratePieChart ()
        {
            pieChart.Reset ();
            pieChart.IsLoading = true;

            foreach (var project in summaryReport.Projects) {
                var slice = new PieSlice ();
                slice.Value = project.TotalTime;
                slice.Color = Color.ParseColor (HexColors [project.Color % HexColors.Length]); 
                pieChart.AddSlice (slice);
            }
            pieChart.IsLoading = false;
        }

        private async Task LoadData ()
        {
            summaryReport = new SummaryReportView ();
            summaryReport.Period = ZoomLevel.Week;
            await summaryReport.Load (backDate);
            var user = ServiceContainer.Resolve<AuthManager> ().User;
        }

        public class SliceListener : PieChart.IOnSliceClickedListener
        {
            public void OnClick (int index)
            {
            }
        }

        public void ListViewHeight (ListView listView)
        {
            ListAdapter = listView.Adapter;
            if (ListAdapter == null)
                return;

            int desiredWidth = ViewGroup.MeasureSpec.MakeMeasureSpec (listView.Width, MeasureSpecMode.Unspecified);
            int totalHeight = 0;
            View view = null;

            for (int i = 0; i < ListAdapter.Count; i++) {
                view = ListAdapter.GetView (i, view, listView);

                view.Measure (desiredWidth, 0);

                totalHeight += view.MeasuredHeight;
            }
            ViewGroup.LayoutParams parameters = listView.LayoutParameters;
            parameters.Height = totalHeight + (listView.DividerHeight * (ListAdapter.Count - 1));
            listView.LayoutParameters = parameters;
            listView.RequestLayout ();
        }

        private string FormattedDateSelector ()
        {
            if (backDate == 0) {
                if (summaryReport.Period == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsThisWeek);
                } else if (summaryReport.Period == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsThisMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsThisYear);
                }
            } else if (backDate == 1) {
                if (summaryReport.Period == ZoomLevel.Week) {
                    return Resources.GetString (Resource.String.ReportsLastWeek);
                } else if (summaryReport.Period == ZoomLevel.Month) {
                    return Resources.GetString (Resource.String.ReportsLastMonth);
                } else {
                    return Resources.GetString (Resource.String.ReportsLastYear);
                }
            } else {
                var startDate = summaryReport.ResolveStartDate (backDate);
                var endDate = summaryReport.ResolveEndDate (startDate);
                if (summaryReport.Period == ZoomLevel.Week) {
                    return String.Format ("{0:MMM dd}th - {1:MMM dd}th", startDate, endDate);
                }
            }
            return "";
        }

    }

    public class ProjectListAdapter : BaseAdapter
    {
        List<ReportProject> dataView;
        private View ColorSquare;
        private TextView ProjectName;
        private TextView ProjectDuration;
        private int focus = -1;

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        public ProjectListAdapter (List<ReportProject> dataView)
        {
            this.dataView = dataView;
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (Resource.Layout.ReportsProjectListItem, parent, false);
            ProjectName = view.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);
            ColorSquare = view.FindViewById<View> (Resource.Id.ColorSquare);
            ProjectDuration = view.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont (Font.Roboto);

            ProjectName.Text = dataView [position].Project;
            ProjectDuration.Text = FormatMilliseconds (dataView [position].TotalTime);
            var SquareDrawable = new GradientDrawable ();
            SquareDrawable.SetCornerRadius (5);
            SquareDrawable.SetColor (Color.ParseColor (HexColors [dataView [position].Color % HexColors.Length]));

            if (focus == position) {
                SquareDrawable.SetShape (ShapeType.Oval);
            } else if (focus != -1 && focus != position) {
                SquareDrawable.SetShape (ShapeType.Rectangle);
                SquareDrawable.SetAlpha (150);
                ProjectName.SetTextColor (Color.LightGray);
                ProjectDuration.SetTextColor (Color.LightGray);
            }
            ColorSquare.SetBackgroundDrawable (SquareDrawable);
            return view;
        }

        public void SetFocus (int selected)
        {
            focus = selected;
            NotifyDataSetChanged ();
        }

        public override int Count {
            get {
                return dataView.Count;
            }
        }

        private string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor (timeSpan.TotalHours).ToString ("00"), timeSpan);
        }
    }
}
