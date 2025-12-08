import React from "react";
import { JsonEditor } from "jsoneditor-react";
import "jsoneditor-react/es/editor.min.css";

interface JSONEditorProps {
  value: any;
  onChange: (json: any) => void;
  mode?: "code" | "tree";
  enableSort?: boolean;
  enableTransform?: boolean;
  uniqueKey: string;
  testId?: string;
  expandOnStart?: boolean;
}

const JSONEditor = ({
  value,
  onChange,
  mode = "code",
  enableSort = false,
  enableTransform = false,
  testId,
  uniqueKey,
  expandOnStart = true,
}: JSONEditorProps) => {
  return (
    <JsonEditor
      key={uniqueKey}
      value={value}
      onChange={(json: any) => {
        onChange(json);
      }}
      mode={mode}
      enableSort={enableSort}
      enableTransform={enableTransform}
      data-testid={testId}
      expandOnStart={expandOnStart}
      search={false}
    />
  );
};

export default JSONEditor;
