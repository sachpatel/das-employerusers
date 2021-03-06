﻿using System.Web.Optimization;

namespace SFA.DAS.EmployerUsers.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*",
                        "~/Scripts/additional-methods.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts").Include(
                "~/scripts/govuk-template.js"));

            bundles.Add(new StyleBundle("~/bundles/screenie6").Include("~/dist/css/screen-ie6.css"));
            bundles.Add(new StyleBundle("~/bundles/screenie7").Include("~/dist/css/screen-ie7.css"));
            bundles.Add(new StyleBundle("~/bundles/screenie8").Include("~/dist/css/screen-ie8.css"));
            bundles.Add(new StyleBundle("~/bundles/screen").Include("~/dist/css/screen.css"));

            bundles.Add(new ScriptBundle("~/bundles/sfajs").Include(
                      "~/dist/javascripts/jquery-1.11.0.min.js",
                      "~/dist/javascripts/govuk-template.js",
                      "~/dist/javascripts/selection-buttons.js",
                      "~/dist/javascripts/app.js"));

            bundles.Add(new ScriptBundle("~/bundles/password").Include(
                      "~/dist/javascripts/zxcvbn.js",
                      "~/dist/javascripts/password.js"));
        }
    }
}
