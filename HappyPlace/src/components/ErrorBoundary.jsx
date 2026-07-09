import React from 'react';
import ErrorFallback from 'src/components/ErrorFallback';

export default class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }
  static getDerivedStateFromError() {
    return { hasError: true };
  }
  componentDidCatch(error, errorInfo) {
    console.error(error, errorInfo);
  }
  handleRetry = () => {
    this.setState({ hasError: false });
  };
  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;
      return <ErrorFallback onRetry={this.handleRetry} />;
    }
    return this.props.children;
  }
}