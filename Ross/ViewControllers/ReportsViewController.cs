﻿using System;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class ReportsViewController : UIViewController
    {
        private ZoomLevel _zoomLevel;

        public ZoomLevel ZoomLevel
        {
            get {
                return _zoomLevel;
            } set {
                if (_zoomLevel == value) {
                    return;
                }
                _zoomLevel = value;
                scrollView.RefreshVisibleReportView ();
            }
        }

        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private TopBorder topBorder;
        private SummaryReportView dataSource;
        private InfiniteScrollView scrollView;
        private int _timeSpaceIndex;

        const float padding  = 24;
        const float navBarHeight = 64;
        const float selectorHeight = 50;


        public ReportsViewController ()
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            menuController = new ReportsMenuController ();
            dataSource = new SummaryReportView ();

            _zoomLevel = ZoomLevel.Week;
            _timeSpaceIndex = 0;
        }

        public override void ViewWillDisappear (bool animated)
        {
            NavigationController.InteractivePopGestureRecognizer.Enabled = true;
            SummaryReportView.SaveReportsState (ZoomLevel);
            base.ViewWillDisappear (animated);
        }

        public override void ViewDidDisappear (bool animated)
        {
            base.ViewDidDisappear (animated);
            if (menuController != null) {
                menuController.Detach ();
                menuController = null;
            }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            _zoomLevel = SummaryReportView.GetLastZoomViewed ();
            View.BackgroundColor = UIColor.White;
            menuController.Attach (this);

            topBorder = new TopBorder ();
            dateSelectorView = new DateSelectorView ();
            dateSelectorView.LeftArrowPressed += (sender, e) => scrollView.SetPageIndex (-1, true);
            dateSelectorView.RightArrowPressed += (sender, e) => {
                if ( _timeSpaceIndex >= 1) { return; }
                scrollView.SetPageIndex ( 1, true);
            };

            scrollView = new InfiniteScrollView ();
            scrollView.OnChangeReport += (sender, e) => {
                _timeSpaceIndex = scrollView.PageIndex;
                var reportView = scrollView.VisibleReportView;
                reportView.ZoomLevel = ZoomLevel;
                reportView.TimeSpaceIndex = _timeSpaceIndex;
                reportView.LoadData();
                ChangeReportState();
            };

            Add (scrollView);
            Add (dateSelectorView);
            Add (topBorder);

            NavigationController.InteractivePopGestureRecognizer.Enabled = false;
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();
            topBorder.Frame = new RectangleF (0.0f, 0.0f, View.Bounds.Width, 2.0f);
            dateSelectorView.Frame = new RectangleF (0, View.Bounds.Height - selectorHeight, View.Bounds.Width, selectorHeight);
            scrollView.Frame = new RectangleF (0.0f, 0.0f, View.Bounds.Width, View.Bounds.Height - selectorHeight);
        }

        public override void LoadView ()
        {
            View = new UIView ().Apply (Style.Screen);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Reports";
        }

        private void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate (_timeSpaceIndex);
        }

        private string FormattedIntervalDate (int backDate)
        {
            string result = "";

            if (backDate == 0) {
                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    result = "ReportsThisWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "ReportsThisMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "ReportsThisYearSelector".Tr ();
                    break;
                }
            } else if (backDate == -1) {
                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    result = "ReportsLastWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "ReportsLastMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "ReportsLastYearSelector".Tr ();
                    break;
                }
            } else {
                var startDate = dataSource.ResolveStartDate (_timeSpaceIndex);
                var endDate = dataSource.ResolveEndDate (startDate);

                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    if (startDate.Month == endDate.Month) {
                        result = startDate.ToString ("ReportsStartWeekInterval".Tr ()) + " - " + endDate.ToString ("ReportsEndWeekInterval".Tr ());
                    } else {
                        result = startDate.Day + "th " + startDate.ToString ("MMM") + " - " + endDate.Day + "th " + startDate.ToString ("MMM");
                    }
                    break;
                case ZoomLevel.Month:
                    result = startDate.ToString ("ReportsMonthInterval".Tr ());
                    break;
                case ZoomLevel.Year:
                    result = startDate.ToString ("ReportsYearInterval".Tr ());
                    break;
                }
            }
            return result;
        }

        internal class TopBorder : UIView
        {
            public TopBorder ()
            {
                BackgroundColor = UIColor.Clear;
            }

            public override void Draw (RectangleF rect)
            {
                using (CGContext g = UIGraphics.GetCurrentContext()) {
                    Color.TimeBarBoderColor.SetColor ();
                    g.FillRect (new RectangleF (0.0f, 0.0f, rect.Width, 1.0f / ContentScaleFactor));
                }
            }
        }
    }
}