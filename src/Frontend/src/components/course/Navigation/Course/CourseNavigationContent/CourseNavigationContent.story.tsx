import React from "react";
import CourseNavigationContent from "./CourseNavigationContent";
import { CourseMenuItem } from "../../types";
import type { Story } from "@storybook/react";
import { DesktopWrapper, disableViewport, getCourseModules } from "../../stroies.data";

export default {
	title: "CourseNavigationHeader",
	...disableViewport,
};

const Template: Story<CourseMenuItem[]> = (items) => (
	<DesktopWrapper>
		<CourseNavigationContent courseId={'courseId'} items={ items } getRefToActive={ React.createRef() }/>
	</DesktopWrapper>
);

const Default = Template.bind({});
Default.args = getCourseModules();
