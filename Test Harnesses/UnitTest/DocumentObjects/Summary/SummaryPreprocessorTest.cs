﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using NUnit.Framework;

using GateKeeper.Common;
using GateKeeper.DocumentObjects.Summary;
using GKManagers.Preprocessors;
using GateKeeper.DocumentObjects;

namespace GateKeeper.UnitTest.DocumentObjects.Summary
{
    /// <summary>
    /// Tests for the SummaryPreprocessor.Validate method
    /// </summary>
    [TestFixture]
    class SummaryPreprocessorValidateTest
    {
        HistoryEntryWriter fakeInfoWriter = delegate (string message) { Console.Write(message); };
        HistoryEntryWriter fakeWarningWriter = delegate (string message) { Console.Write(message); };

        // Skeleton of a valid summary.
        const string VALID_SUMMARY = @"
<Summary id=""CDR000000001"" LegacyPDQID=""1278"">
    <SummarySection id=""_1"">
        <Para id = ""_90"" >Sub-section</Para>
    </SummarySection>
    <SummarySection id=""_2"">
        <Para id = ""_91"" >Sub-section 2</Para>
    </SummarySection>
    <SummarySection id=""_AboutThis_1"">
        <Title>About This PDQ Summary</Title>
    </SummarySection>
</Summary>
";


        // Not actually a summary.
        const string GLOSSARY_TERM = @"
<GlossaryTerm id=""CDR000000001"" LegacyPDQID=""1278"">
</GlossaryTerm>
";

        SplitDataManager MatchingSplitData; // Split manager which matches VALID_SUMMARY
        SplitDataManager UnmatchedSplitData;// Split manager which does NOT matches VALID_SUMMARY
        SplitDataManager incorrectGISplitData;// Split manager with a non-existant section identified as General Information.

        [TestFixtureSetUp]
        public void Setup()
        {
            // Split manager which matches VALID_SUMMARY
            MatchingSplitData = SplitDataManager.CreateFromString(@"
[
	{
		""comment"": ""Split manager which matches VALID_SUMMARY"",

        ""cdrid"": ""1"",
        ""url"": ""Not sure what goes here"",
        ""page-sections"": [""_1"", ""_2"", ""_AboutThis_1""],
		""general-sections"": [""_1""],
		""linked-sections"": [""_1"", ""_90""],
		""long-title"": ""The long title for the pilot page"",
		""short-title"": ""The short title for the summary's pilot page"",
		""long-description"": ""The pilot page's long description"",
		""meta-keywords"": ""keyword1 keyword2""
	}
]    
");

            // Split manager which does NOT match VALID_SUMMARY
            UnmatchedSplitData = SplitDataManager.CreateFromString(@"
[
	{
		""comment"": ""Split manager which does NOT match VALID_SUMMARY"",

        ""cdrid"": ""2"",
        ""url"": ""Not sure what goes here"",
        ""page-sections"": [""_1"", ""_2"", ""_AboutThis_1""],
		""general-sections"": [""_1""],
		""linked-sections"": [""_1"", ""_90""],
		""long-title"": ""The long title for the pilot page"",
		""short-title"": ""The short title for the summary's pilot page"",
		""long-description"": ""The pilot page's long description"",
		""meta-keywords"": ""keyword1 keyword2""
	}
]    
");


            // Split manager with a non-existant section identified as General Information.
            incorrectGISplitData = SplitDataManager.CreateFromString(@"
[
	{
		""comment"": ""Split manager with a non-existant section identified as General Information."",

        ""cdrid"": ""1"",
        ""url"": ""Not sure what goes here"",
        ""page-sections"": [""_1"", ""_2"", ""_AboutThis_1""],
		""general-sections"": [""_7""],
		""linked-sections"": [""_1"", ""_90""],
		""long-title"": ""The long title for the pilot page"",
		""short-title"": ""The short title for the summary's pilot page"",
		""long-description"": ""The pilot page's long description"",
		""meta-keywords"": ""keyword1 keyword2""
	}
]    
");


        }


        /// <summary>
        /// Test that sumamry with a valid root element is allowed.
        /// </summary>
        [Test]
        public void ValidSummaryRoot()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(VALID_SUMMARY);
            ISplitDataManager splitMgr = new MockSplitDataManager();
            SummaryPreprocessor processor = new SummaryPreprocessor();
            Assert.DoesNotThrow(() => { processor.Validate(doc, splitMgr); });
        }

        /// <summary>
        /// Test that sumamry with an invalid root element fails.
        /// </summary>
        [Test]
        public void FailInvalidSummaryRoot()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GLOSSARY_TERM);
            ISplitDataManager splitMgr = new MockSplitDataManager();
            SummaryPreprocessor processor = new SummaryPreprocessor();
            Assert.Throws<ValidationException>(() => { processor.Validate(doc, splitMgr); });
        }

        /// <summary>
        /// Test that split data that matches the summary is accepted.
        /// </summary>
        [Test]
        public void ValidSplit()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(VALID_SUMMARY);
            ISplitDataManager splitMgr = MatchingSplitData;
            SummaryPreprocessor processor = new SummaryPreprocessor();
            Assert.DoesNotThrow(() => { processor.Validate(doc, splitMgr); });
        }

        /// <summary>
        /// Test that a summary which doesn't exist in the split data is allowed.
        /// </summary>
        [Test]
        public void UnmatchedSplit()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(VALID_SUMMARY);
            ISplitDataManager splitMgr = UnmatchedSplitData;
            SummaryPreprocessor processor = new SummaryPreprocessor();
            Assert.DoesNotThrow(() => { processor.Validate(doc, splitMgr); });
        }


        /// <summary>
        /// Test that processing fails for split data with a non-existant section identified as General Information.
        /// </summary>
        [Test]
        public void FailNonExistantGISection()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(VALID_SUMMARY);
            ISplitDataManager splitMgr = UnmatchedSplitData;
            SummaryPreprocessor processor = new SummaryPreprocessor();
            Assert.Throws<ValidationException>(() => { processor.Validate(doc, splitMgr); });
        }
    }
}